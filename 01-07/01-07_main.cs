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

		//1:トーラス
		Vertex[] vertices1;			//頂点
		int[] indices1;				//頂点の指標
		int vbo1;					//VBOのバッファの識別番号を保持
		int ibo1;					//IBOのバッファの識別番号を保持
		int vao1;					//VAOの識別番号を保持

		//2:球
		Vertex[] vertices2;			//頂点
		int[] indices2;				//頂点の指標
		int vbo2;					//VBOのバッファの識別番号を保持
		int ibo2;					//IBOのバッファの識別番号を保持
		int vao2;					//VAOの識別番号を保持

		//800x600のウィンドウを作る。タイトルは「1-7:Vertex Array Object」
		public Game()
			: base(800, 600, GraphicsMode.Default, "1-7:Vertex Array Object")
		{
			lightPosition = new Vector4(200.0f, 150f, 500.0f, 0.0f);
			lightAmbient = new Color4(0.2f, 0.2f, 0.2f, 1.0f);
			lightDiffuse = new Color4(0.7f, 0.7f, 0.7f, 1.0f);
			lightSpecular = new Color4(1.0f, 1.0f, 1.0f, 1.0f);

			materialAmbient = new Color4(0.2f, 0.2f, 0.2f, 1.0f);
			//materialDiffuse = new Color4(0.7f, 0.7f, 0.7f, 1.0f);
			materialSpecular = new Color4(0.6f, 0.6f, 0.6f, 1.0f);
			materialShininess = 51.4f;

			vbo1 = 0;
			ibo1 = 0;
			vao1 = 0;
			vbo2 = 0;
			ibo2 = 0;
			vao2 = 0;

			InitTorus(16, 32, 0.4, 1.0);
			InitSphere(16, 16, 1.0f);

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

			//VBOを1コ生成し、1の頂点データを送り込む
			GL.GenBuffers(1, out vbo1);
			GL.BindBuffer(BufferTarget.ArrayBuffer, vbo1);
			int vertexArray1Size = vertices1.Length * Vertex.Size;
			GL.BufferData<Vertex>(BufferTarget.ArrayBuffer, new IntPtr(vertexArray1Size), vertices1, BufferUsageHint.StaticDraw);
			GL.BindBuffer(BufferTarget.ArrayBuffer, 0);

			//IBOを1コ生成し、1のインデックスデータを送り込む
			GL.GenBuffers(1, out ibo1);
			GL.BindBuffer(BufferTarget.ElementArrayBuffer, ibo1);
			int indexArray1Size = indices1.Length * sizeof(int);
			GL.BufferData(BufferTarget.ElementArrayBuffer, new IntPtr(indexArray1Size), indices1, BufferUsageHint.StaticDraw);
			GL.BindBuffer(BufferTarget.ElementArrayBuffer, 0);

			//VBOを1コ生成し、2の頂点データを送り込む
			GL.GenBuffers(1, out vbo2);
			GL.BindBuffer(BufferTarget.ArrayBuffer, vbo2);
			int vertexArray2Size = vertices2.Length * Vertex.Size;
			GL.BufferData<Vertex>(BufferTarget.ArrayBuffer, new IntPtr(vertexArray2Size), vertices2, BufferUsageHint.StaticDraw);
			GL.BindBuffer(BufferTarget.ArrayBuffer, 0);

			//IBOを1コ生成し、2のインデックスデータを送り込む
			GL.GenBuffers(1, out ibo2);
			GL.BindBuffer(BufferTarget.ElementArrayBuffer, ibo2);
			int indexArray2Size = indices2.Length * sizeof(int);
			GL.BufferData(BufferTarget.ElementArrayBuffer, new IntPtr(indexArray2Size), indices2, BufferUsageHint.StaticDraw);
			GL.BindBuffer(BufferTarget.ElementArrayBuffer, 0);


			//VAOを1コ作成
			GL.GenVertexArrays(1, out vao1);

			//ここからVAO1
			GL.BindVertexArray(vao1);

			//各Arrayを有効化
			GL.EnableClientState(ArrayCap.VertexArray);
			GL.EnableClientState(ArrayCap.NormalArray);
			GL.EnableClientState(ArrayCap.ColorArray);

			GL.BindBuffer(BufferTarget.ArrayBuffer, vbo1);

			//頂点の位置情報の場所を指定
			GL.VertexPointer(3, VertexPointerType.Float, Vertex.Size, 0);

			//頂点の法線情報の場所を指定
			GL.NormalPointer(NormalPointerType.Float, Vertex.Size, Vector3.SizeInBytes);

			//頂点の色情報の場所を指定
			GL.ColorPointer(4, ColorPointerType.Float, Vertex.Size, Vector3.SizeInBytes * 2);

			GL.BindBuffer(BufferTarget.ArrayBuffer, 0);

			GL.BindVertexArray(0);
			//VAO1ここまで


			//VAOを1コ作成
			GL.GenVertexArrays(1, out vao2);

			//ここからVAO2
			GL.BindVertexArray(vao2);

			//各Arrayを有効化
			GL.EnableClientState(ArrayCap.VertexArray);
			GL.EnableClientState(ArrayCap.NormalArray);
			GL.EnableClientState(ArrayCap.ColorArray);

			GL.BindBuffer(BufferTarget.ArrayBuffer, vbo2);

			//頂点の位置情報の場所を指定
			GL.VertexPointer(3, VertexPointerType.Float, Vertex.Size, 0);

			//頂点の法線情報の場所を指定
			GL.NormalPointer(NormalPointerType.Float, Vertex.Size, Vector3.SizeInBytes);

			//頂点の色情報の場所を指定
			GL.ColorPointer(4, ColorPointerType.Float, Vertex.Size, Vector3.SizeInBytes * 2);

			GL.BindBuffer(BufferTarget.ArrayBuffer, 0);

			GL.BindVertexArray(0);
			//VAO2ここまで
		}

		//ウィンドウの終了時に実行される。
		protected override void OnUnload(EventArgs e)
		{
			base.OnUnload(e);

			GL.DeleteBuffers(1, ref vbo1);			//バッファを1コ削除
			GL.DeleteBuffers(1, ref ibo1);			//バッファを1コ削除
			GL.DeleteVertexArrays(1, ref vao1);		//VAOを1コ削除

			GL.DeleteBuffers(1, ref vbo2);			//バッファを1コ削除
			GL.DeleteBuffers(1, ref ibo2);			//バッファを1コ削除
			GL.DeleteVertexArrays(1, ref vao2);		//VAOを1コ削除

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

			GL.MatrixMode(MatrixMode.Modelview);

			GL.PushMatrix();
			GL.Translate(-1.5f, 0.0f, 0.0f);

			//1を描画
			GL.BindVertexArray(vao1);
			GL.BindBuffer(BufferTarget.ElementArrayBuffer, ibo1);
			GL.DrawElements(BeginMode.Quads, indices1.Length, DrawElementsType.UnsignedInt, 0);

			GL.PopMatrix();
			GL.Translate(1.5f, 0.0f, 0.0f);

			//2を描画
			GL.BindVertexArray(vao2);
			GL.BindBuffer(BufferTarget.ElementArrayBuffer, ibo2);
			GL.DrawElements(BeginMode.Quads, indices2.Length, DrawElementsType.UnsignedInt, 0);

			GL.BindVertexArray(0);

			SwapBuffers();
		}

		//1をトーラスで初期化
		void InitTorus(int row, int column, double smallRadius, double largeRadius)
		{
			LinkedList<Vertex> vertexList = new LinkedList<Vertex>();
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

					Vector3 position = new Vector3((float)px, (float)py, (float)pz);
					Vector3 normal = new Vector3((float)nx, (float)ny, (float)nz);
					Color4 color = RGBAfromHSVA(360f * (float)j / (float)column, 1.0f, 1.0f, 1.0f);
					vertexList.AddLast(new Vertex(position, normal, color));
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

			vertices1 = vertexList.ToArray();
			indices1 = indexList.ToArray();
		}

		//2を球で初期化
		void InitSphere(int slice, int stack, float radius)
		{
			LinkedList<Vertex> vertexList = new LinkedList<Vertex>();
			LinkedList<int> indexList = new LinkedList<int>();

			for (int i = 0; i <= stack; i++)
			{
				double p = Math.PI / stack * i;
				double pHeight = Math.Cos(p);
				double pWidth = Math.Sin(p);

				for (int j = 0; j <= slice; j++)
				{
					double rotor = 2 * Math.PI / slice * j;
					double x = Math.Cos(rotor);
					double y = Math.Sin(rotor);

					Vector3 position = new Vector3((float)(radius * x * pWidth), (float)(radius * pHeight), (float)(radius * y * pWidth));
					Vector3 normal = new Vector3((float)(x * pWidth), (float)pHeight, (float)(y * pWidth));
					Color4 color = RGBAfromHSVA(360f * (float)j / (float)slice, 1.0f, 1.0f, 1.0f);
					vertexList.AddLast(new Vertex(position, normal, color));
				}
			}

			for (int i = 0; i <= stack; i++)
			{
				for (int j = 0; j <= slice; j++)
				{
					int d = i * (slice + 1) + j;
					indexList.AddLast(d);
					indexList.AddLast(d + 1);
					indexList.AddLast(d + slice + 2);
					indexList.AddLast(d + slice + 1);
				}
			}
			vertices2 = vertexList.ToArray();
			indices2 = indexList.ToArray();
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