/*
 * 参照設定 : OpenTK System System.Drawing
 */

using System;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using OpenTK.Input;

using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace OpenTK_Samples
{
	struct Vertex
	{
		public Vector3 position;
		public Vector3 normal;
		public Color4 color;

		public Vertex(Vector3 position, Vector3 normal, Color4 color)
		{
			this.position = position;
			this.normal = normal;
			this.color = color;
		}

		public static readonly int Size = Marshal.SizeOf(default(Vertex));
	}

	class OpenTK_Sample
	{
		[STAThread]
		static int Main()
		{
			using (Game window = new Game())
			{
				window.Run(30.0);
			}
			return 0;
		}
	}

	class Game : GameWindow
	{
		#region Camera__Field

		bool isCameraRotating;		//カメラが回転状態かどうか
		Vector2 current, previous;	//現在の点、前の点
		Matrix4 rotate;				//回転行列
		float zoom;					//拡大度
		float wheelPrevious;		//マウスホイールの前の状態

		#endregion

		Vector4 lightPosition;		//平行光源の方向
		Color4 lightAmbient;		//光源の環境光成分
		Color4 lightDiffuse;		//光源の拡散光成分
		Color4 lightSpecular;		//光源の鏡面光成分

		Color4 materialAmbient;		//材質の環境光成分
		//Color4 materialDiffuse;	//材質の拡散光成分
		Color4 materialSpecular;	//材質の鏡面光成分
		float materialShininess;	//材質の鏡面光の鋭さ

		Vertex[] vertices;			//頂点
		int[] indices;				//頂点の指標

		int vbo;					//VBOのバッファの識別番号を保持
		int ibo;					//IBOのバッファの識別番号を保持

		DateTime start;				//プログラムの開始時刻を保持

		//800x600のウィンドウを作る。タイトルは「1-6:VBO(3)」
		public Game()
			: base(800, 600, GraphicsMode.Default, "1-6:VBO(3)")
		{
			start = DateTime.Now;

			lightPosition = new Vector4(200.0f, 150f, 500.0f, 0.0f);
			lightAmbient = new Color4(0.2f, 0.2f, 0.2f, 1.0f);
			lightDiffuse = new Color4(0.7f, 0.7f, 0.7f, 1.0f);
			lightSpecular = new Color4(1.0f, 1.0f, 1.0f, 1.0f);

			materialAmbient = new Color4(0.2f, 0.2f, 0.2f, 1.0f);
			//materialDiffuse = new Color4(0.7f, 0.7f, 0.7f, 1.0f);
			materialSpecular = new Color4(0.6f, 0.6f, 0.6f, 1.0f);
			materialShininess = 51.4f;

			vbo = 0;
			ibo = 0;

			#region Camera__Initialize

			isCameraRotating = false;
			current = Vector2.Zero;
			previous = Vector2.Zero;
			rotate = Matrix4.Identity;
			zoom = 1.0f;
			wheelPrevious = 0.0f;

			#endregion

			#region Camera__Event

			//マウスボタンが押されると発生するイベント
			this.Mouse.ButtonDown += (sender, e) =>
			{
				//右ボタンが押された場合
				if (e.Button == MouseButton.Right)
				{
					isCameraRotating = true;
					current = new Vector2(this.Mouse.X, this.Mouse.Y);
				}
			};

			//マウスボタンが離されると発生するイベント
			this.Mouse.ButtonUp += (sender, e) =>
			{
				//右ボタンが押された場合
				if (e.Button == MouseButton.Right)
				{
					isCameraRotating = false;
					previous = Vector2.Zero;
				}
			};

			//マウスが動くと発生するイベント
			this.Mouse.Move += (sender, e) =>
			{
				////カメラが回転状態の場合
				if (isCameraRotating)
				{
					previous = current;
					current = new Vector2(this.Mouse.X, this.Mouse.Y);
					Vector2 delta = current - previous;
					delta /= (float)Math.Sqrt(this.Width * this.Width + this.Height * this.Height);
					float length = delta.Length;
					if (length > 0.0)
					{
						float rad = length * MathHelper.Pi;
						float theta = (float)Math.Sin(rad) / length;
						Quaternion after = new Quaternion(delta.Y * theta, delta.X * theta, 0.0f, (float)Math.Cos(rad));
						rotate = rotate * Matrix4.Rotate(after);
					}
				}
			};

			//マウスホイールが回転すると発生するイベント
			this.Mouse.WheelChanged += (sender, e) =>
			{
				float delta = (float)this.Mouse.Wheel - (float)wheelPrevious;

				zoom *= (float)Math.Pow(1.2, delta);

				//拡大、縮小の制限
				if (zoom > 2.0f)
					zoom = 2.0f;
				if (zoom < 0.5f)
					zoom = 0.5f;
				wheelPrevious = this.Mouse.Wheel;
			};

			#endregion

			VSync = VSyncMode.On;
		}

		//ウィンドウの起動時に実行される。
		protected override void OnLoad(EventArgs e)
		{
			base.OnLoad(e);

			GL.ClearColor(Color4.Black);
			GL.Enable(EnableCap.DepthTest);

			//裏面削除、反時計回りが表でカリング
			GL.Enable(EnableCap.CullFace);
			GL.CullFace(CullFaceMode.Back);
			GL.FrontFace(FrontFaceDirection.Ccw);

			//ライティングON Light0を有効化
			GL.Enable(EnableCap.Lighting);
			GL.Enable(EnableCap.Light0);

			//法線の正規化
			GL.Enable(EnableCap.Normalize);

			//色を材質に変換
			GL.Enable(EnableCap.ColorMaterial);
			GL.ColorMaterial(MaterialFace.Front, ColorMaterialParameter.Diffuse);

			//各Arrayを有効化
			GL.EnableClientState(ArrayCap.VertexArray);
			GL.EnableClientState(ArrayCap.NormalArray);
			GL.EnableClientState(ArrayCap.ColorArray);

			//バッファを1コ生成
			GL.GenBuffers(1, out vbo);

			//バッファを1コ生成
			GL.GenBuffers(1, out ibo);
		}

		//ウィンドウの終了時に実行される。
		protected override void OnUnload(EventArgs e)
		{
			base.OnUnload(e);

			GL.DeleteBuffers(1, ref vbo);		//バッファを1コ削除
			GL.DeleteBuffers(1, ref ibo);		//バッファを1コ削除

			GL.DisableClientState(ArrayCap.VertexArray);	//VertexArrayを無効化
			GL.DisableClientState(ArrayCap.NormalArray);	//NormalArrayを無効化
			GL.DisableClientState(ArrayCap.ColorArray);		//ColorArrayを無効化
		}

		//ウィンドウのサイズが変更された場合に実行される。
		protected override void OnResize(EventArgs e)
		{
			base.OnResize(e);

			GL.Viewport(ClientRectangle);
		}

		//画面更新で実行される。
		protected override void OnUpdateFrame(FrameEventArgs e)
		{
			base.OnUpdateFrame(e);

			//Escapeキーで終了
			if (Keyboard[Key.Escape])
			{
				this.Exit();
			}

			#region Camera__Keyboard

			//F1キーで回転をリセット
			if (Keyboard[Key.F1])
			{
				rotate = Matrix4.Identity;
			}

			//F2キーでY軸90度回転
			if (Keyboard[Key.F2])
			{
				rotate = Matrix4.CreateRotationY(MathHelper.PiOver2);
			}

			//F3キーでY軸180度回転
			if (Keyboard[Key.F3])
			{
				rotate = Matrix4.CreateRotationY(MathHelper.Pi);
			}

			//F4キーでY軸270度回転
			if (Keyboard[Key.F4])
			{
				rotate = Matrix4.CreateRotationY(MathHelper.ThreePiOver2);
			}

			//F5キーで拡大をリセット
			if (Keyboard[Key.F5])
			{
				zoom = 1.0f;
			}

			#endregion
		}

		//画面描画で実行される。
		protected override void OnRenderFrame(FrameEventArgs e)
		{
			base.OnRenderFrame(e);

			GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

			#region TransFormationMatrix

			Matrix4 modelView = Matrix4.LookAt(Vector3.UnitZ * 10 / zoom, Vector3.Zero, Vector3.UnitY);
			GL.MatrixMode(MatrixMode.Modelview);
			GL.LoadMatrix(ref modelView);
			GL.MultMatrix(ref rotate);

			Matrix4 projection = Matrix4.CreatePerspectiveFieldOfView(MathHelper.PiOver4 / zoom, (float)this.Width / (float)this.Height, 1.0f, 64.0f);
			GL.MatrixMode(MatrixMode.Projection);
			GL.LoadMatrix(ref projection);

			#endregion

			//ライトの指定
			GL.Light(LightName.Light0, LightParameter.Position, lightPosition);
			GL.Light(LightName.Light0, LightParameter.Ambient, lightAmbient);
			GL.Light(LightName.Light0, LightParameter.Diffuse, lightDiffuse);
			GL.Light(LightName.Light0, LightParameter.Specular, lightSpecular);

			//材質の指定
			GL.Material(MaterialFace.Front, MaterialParameter.Ambient, materialAmbient);
			//GL.Material(MaterialFace.Front, MaterialParameter.Diffuse, materialDiffuse);
			GL.Material(MaterialFace.Front, MaterialParameter.Specular, materialSpecular);
			GL.Material(MaterialFace.Front, MaterialParameter.Shininess, materialShininess);

			//波を毎回生成
			CreateWave(100, 100, 1.0f, 0.5f, 1.0f, 1.0f);


			//毎回描画前にデータを送り込む
			GL.BindBuffer(BufferTarget.ArrayBuffer, vbo);
			int vertexArraySize = vertices.Length * Vertex.Size;
			GL.BufferData<Vertex>(BufferTarget.ArrayBuffer, new IntPtr(vertexArraySize), vertices, BufferUsageHint.DynamicDraw);

			GL.BindBuffer(BufferTarget.ElementArrayBuffer, ibo);
			int indexArraySize = indices.Length * sizeof(int);
			GL.BufferData(BufferTarget.ElementArrayBuffer, new IntPtr(indexArraySize), indices, BufferUsageHint.DynamicDraw);


			GL.BindBuffer(BufferTarget.ArrayBuffer, vbo);

			//頂点の位置情報の場所を指定
			GL.VertexPointer(3, VertexPointerType.Float, Vertex.Size, 0);

			//頂点の法線情報の場所を指定
			GL.NormalPointer(NormalPointerType.Float, Vertex.Size, Vector3.SizeInBytes);

			//頂点の色情報の場所を指定
			GL.ColorPointer(4, ColorPointerType.Float, Vertex.Size, Vector3.SizeInBytes * 2);

			//IBOを使って描画
			GL.BindBuffer(BufferTarget.ElementArrayBuffer, ibo);
			GL.DrawElements(BeginMode.Quads, indices.Length, DrawElementsType.UnsignedInt, 0);


			//バッファのひも付けを解除
			GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
			GL.BindBuffer(BufferTarget.ElementArrayBuffer, 0);

			SwapBuffers();
		}

		//波を作成
		void CreateWave(int row, int column, float size, float height,float frequency, float velocity)
		{
			if (row < 1 || column < 1)
			{
				return;
			}

			vertices = new Vertex[(row + 1) * (column + 1)];
			indices = new int[row * column * 4];

			//プログラムの経過時間を取得
			TimeSpan ts = DateTime.Now - start;

			float omega = MathHelper.TwoPi * frequency;
			float t = (float)ts.TotalSeconds;
			float v = velocity;
			float h = (float)height / 2.0f;

			//位置、法線、色
			for (int i = 0; i <= row; i++)
			{
				for (int j = 0; j <= column; j++)
				{
					float x = (2.0f * (float)i / (float)(row + 1) - 1.0f) * size;
					float y = (2.0f * (float)j / (float)(column + 1) - 1.0f) * size;
					float r = x * x + y * y;
					
					//z = h * sin(omega(t-r/v))
					//z'= h * (-omega/v)cos(2pif(t-r/v)) 接線
					float z = h * (float)Math.Sin(omega * (t - r / v));
					Vector3 position = new Vector3(x, y, z);
					Vector3 n = new Vector3(h * (float)Math.Cos(omega * (t - r / v)) * omega / v, 0.0f, 1.0f);	//xz平面での法線
					Matrix4 m = Matrix4.CreateRotationZ((float)Math.Atan2(y, x));
					Vector3 normal = Vector3.Transform(n, m);
					normal.Normalize();
					Color4 color = new Color4((float)i / (float)(row + 1), (float)j / (float)(column + 1), (z + 1.0f) * 0.5f, 1.0f);
					vertices[i * (column + 1) + j] = new Vertex(position, normal, color);
				}
			}

			//インデックス
			for (int i = 0; i < row; i++)
			{
				for (int j = 0; j < column; j++)
				{
					indices[(i * column + j) * 4] = i * (column + 1) + j;
					indices[(i * column + j) * 4 + 1] = (i + 1) * (column + 1) + j;
					indices[(i * column + j) * 4 + 2] = (i + 1) * (column + 1) + j + 1;
					indices[(i * column + j) * 4 + 3] = i * (column + 1) + j + 1;
				}
			}
		}
	}
}