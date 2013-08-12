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

		Vector3[] positions;		//頂点の位置
		Vector3[] normals;			//頂点の法線
		Color4[] colors;			//頂点の色
		int[] indices;				//頂点の指標

		int[] vbo;					//VBOのバッファの識別番号を保持 0:position 1:normal 2:color
		int ibo;					//IBOのバッファの識別番号を保持

		//800x600のウィンドウを作る。タイトルは「1-5:IBO & VBO(2)」
		public Game()
			: base(800, 600, GraphicsMode.Default, "1-5:IBO & VBO(2)")
		{
			lightPosition = new Vector4(200.0f, 150f, 500.0f, 0.0f);
			lightAmbient = new Color4(0.2f, 0.2f, 0.2f, 1.0f);
			lightDiffuse = new Color4(0.7f, 0.7f, 0.7f, 1.0f);
			lightSpecular = new Color4(1.0f, 1.0f, 1.0f, 1.0f);

			materialAmbient = new Color4(0.2f, 0.2f, 0.2f, 1.0f);
			//materialDiffuse = new Color4(0.7f, 0.7f, 0.7f, 1.0f);
			materialSpecular = new Color4(0.6f, 0.6f, 0.6f, 1.0f);
			materialShininess = 51.4f;

			InitTorus(16, 32, 0.4, 1.0);
			vbo = new int[3];
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

			//バッファを3コ生成
			GL.GenBuffers(3, vbo);

			GL.BindBuffer(BufferTarget.ArrayBuffer, vbo[0]);
			int positionArraySize = positions.Length * Marshal.SizeOf(default(Vector3));
			GL.BufferData(BufferTarget.ArrayBuffer, new IntPtr(positionArraySize), positions, BufferUsageHint.StaticDraw);

			GL.BindBuffer(BufferTarget.ArrayBuffer, vbo[1]);
			int normalArraySize = normals.Length * Marshal.SizeOf(default(Vector3));
			GL.BufferData(BufferTarget.ArrayBuffer, new IntPtr(normalArraySize), normals, BufferUsageHint.StaticDraw);

			GL.BindBuffer(BufferTarget.ArrayBuffer, vbo[2]);
			int colorArraySize = colors.Length * Marshal.SizeOf(default(Color4));
			GL.BufferData(BufferTarget.ArrayBuffer, new IntPtr(colorArraySize), colors, BufferUsageHint.StaticDraw);

			//バッファを1コ生成
			GL.GenBuffers(1, out ibo);

			GL.BindBuffer(BufferTarget.ElementArrayBuffer, ibo);
			int indexArraySize = indices.Length * sizeof(int);
			GL.BufferData(BufferTarget.ElementArrayBuffer, new IntPtr(indexArraySize), indices, BufferUsageHint.StaticDraw);

			//バッファのひも付けを解除
			GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
			GL.BindBuffer(BufferTarget.ElementArrayBuffer, 0);
		}

		//ウィンドウの終了時に実行される。
		protected override void OnUnload(EventArgs e)
		{
			base.OnUnload(e);

			GL.DeleteBuffers(3, vbo);			//バッファを3コ削除
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
			

			//頂点の位置情報の場所を指定
			GL.BindBuffer(BufferTarget.ArrayBuffer, vbo[0]);
			GL.VertexPointer(3, VertexPointerType.Float, 0, 0);

			//頂点の法線情報の場所を指定
			GL.BindBuffer(BufferTarget.ArrayBuffer, vbo[1]);
			GL.NormalPointer(NormalPointerType.Float, 0, 0);

			//頂点の色情報の場所を指定
			GL.BindBuffer(BufferTarget.ArrayBuffer, vbo[2]);
			GL.ColorPointer(4, ColorPointerType.Float, 0, 0);

			//IBOを使って描画
			GL.BindBuffer(BufferTarget.ElementArrayBuffer, ibo);
			GL.DrawElements(BeginMode.Quads, indices.Length, DrawElementsType.UnsignedInt, 0);

			GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
			GL.BindBuffer(BufferTarget.ElementArrayBuffer, 0);

			SwapBuffers();
		}

		//トーラスで初期化
		void InitTorus(int row, int column, double smallRadius, double largeRadius)
		{
			LinkedList<Vector3> positionList = new LinkedList<Vector3>();
			LinkedList<Vector3> normalList = new LinkedList<Vector3>();
			LinkedList<Color4> colorList = new LinkedList<Color4>();
			LinkedList<int> indexList = new LinkedList<int>();

			for (int i = 0; i <= row; i++)
			{
				double sr = (2.0 * Math.PI / row) * i;
				double cossr = Math.Cos(sr);
				double sinsr = Math.Sin(sr);

				double sx = cossr * smallRadius;
				double sy = sinsr * smallRadius;
				for (int j = 0; j <= column; j++)
				{
					double lr = (2.0 * Math.PI / column) * j;
					double coslr = Math.Cos(lr);
					double sinlr = Math.Sin(lr);

					double px = coslr * (sx + largeRadius);
					double py = sy;
					double pz = sinlr * (sx + largeRadius);

					double nx = cossr * coslr;
					double ny = sinsr;
					double nz = cossr * sinlr;

					positionList.AddLast(new Vector3((float)px, (float)py, (float)pz));
					normalList.AddLast(new Vector3((float)nx, (float)ny, (float)nz));
					colorList.AddLast(RGBAfromHSVA(360f * (float)j / (float)column, 1.0f, 1.0f, 1.0f));
				}
			}
			for (int i = 0; i < row; i++)
			{
				for (int j = 0; j < column; j++)
				{
					int d = i * (column + 1) + j;
					indexList.AddLast(d);
					indexList.AddLast(d + column + 1);
					indexList.AddLast(d + column + 2);
					indexList.AddLast(d + 1);
				}
			}

			positions = positionList.ToArray();
			normals = normalList.ToArray();
			colors = colorList.ToArray();
			indices = indexList.ToArray();
		}

		static Color4 RGBAfromHSVA(float H, float S, float V, float A)
		{
			Color4 result = new Color4();
			H %= 360;
			int i = ((int)Math.Floor(H / 60));
			float f = H / 60.0f - i;
			float p = V * (1.0f - S);
			float q = V * (1.0f - f * S);
			float t = V * (1.0f - (1.0f - f) * S);
			switch (i)
			{
				case 0:
					result.R = V;
					result.G = t;
					result.B = p;
					break;
				case 1:
					result.R = q;
					result.G = V;
					result.B = p;
					break;
				case 2:
					result.R = p;
					result.G = V;
					result.B = t;
					break;
				case 3:
					result.R = p;
					result.G = q;
					result.B = V;
					break;
				case 4:
					result.R = t;
					result.G = p;
					result.B = V;
					break;
				case 5:
					result.R = V;
					result.G = p;
					result.B = q;
					break;
			}
			return result;
		}
	}
}
