using System;
using System.Drawing;
using System.Drawing.Imaging;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using OpenTK.Input;

using System.Collections.Generic;
using System.Runtime.InteropServices;

/*
 * 参照設定 : OpenTK System System.Drawing
 */

namespace OpenTK_Samples
{
	struct Vertex
	{
		public Vector3 position;
		public Vector3 normal;
		public Vector2 uv;

		public Vertex(Vector3 position, Vector3 normal, Vector2 uv)
		{
			this.position = position;
			this.normal = normal;
			this.uv = uv;
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
		Color4 materialDiffuse;	//材質の拡散光成分
		Color4 materialSpecular;	//材質の鏡面光成分
		float materialShininess;	//材質の鏡面光の鋭さ

		Vertex[] vertices;
		int[] indices;
		int vbo;
		int ibo;
		int vao;
		int texture;

		//800x600のウィンドウを作る。タイトルは「2-12:Texture with VBO」
		public Game()
			: base(800, 600, GraphicsMode.Default, "2-12:Texture with VBO")
		{
			lightPosition = new Vector4(200.0f, 150f, 500.0f, 0.0f);
			lightAmbient = Color4.White;//new Color4(0.2f, 0.2f, 0.2f, 1.0f);
			lightDiffuse = new Color4(0.7f, 0.7f, 0.7f, 1.0f);
			lightSpecular = new Color4(1.0f, 1.0f, 1.0f, 1.0f);

			materialAmbient = new Color4(0.2f, 0.2f, 0.2f, 1.0f);
			materialDiffuse = new Color4(0.7f, 0.7f, 0.7f, 1.0f);
			materialSpecular = new Color4(0.6f, 0.6f, 0.6f, 1.0f);
			materialShininess = 51.4f;

			vbo = 0;
			ibo = 0;
			vao = 0;

			InitDice();

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

			GL.ClearColor(Color4.Gray);
			GL.Enable(EnableCap.DepthTest);

			//裏面削除、時計回りが表でカリング
			GL.Enable(EnableCap.CullFace);
			GL.CullFace(CullFaceMode.Back);
			GL.FrontFace(FrontFaceDirection.Cw);

			//ライティングON Light0を有効化
			GL.Enable(EnableCap.Lighting);
			GL.Enable(EnableCap.Light0);

			//法線の正規化
			GL.Enable(EnableCap.Normalize);

			//VBOを1コ生成し、1の頂点データを送り込む
			GL.GenBuffers(1, out vbo);
			GL.BindBuffer(BufferTarget.ArrayBuffer, vbo);
			GL.BufferData<Vertex>(BufferTarget.ArrayBuffer, new IntPtr(vertices.Length * Vertex.Size), vertices, BufferUsageHint.StaticDraw);
			GL.BindBuffer(BufferTarget.ArrayBuffer, 0);

			//IBOを1コ生成し、1のインデックスデータを送り込む
			GL.GenBuffers(1, out ibo);
			GL.BindBuffer(BufferTarget.ElementArrayBuffer, ibo);
			int indexArray1Size = indices.Length * sizeof(int);
			GL.BufferData(BufferTarget.ElementArrayBuffer, new IntPtr(indices.Length * sizeof(int)), indices, BufferUsageHint.StaticDraw);
			GL.BindBuffer(BufferTarget.ElementArrayBuffer, 0);

			//VAOを1コ作成し、設定
			GL.GenVertexArrays(1, out vao);
			GL.BindVertexArray(vao);

			//各Arrayを有効化
			GL.EnableClientState(ArrayCap.VertexArray);
			GL.EnableClientState(ArrayCap.NormalArray);
			GL.EnableClientState(ArrayCap.TextureCoordArray);

			GL.BindBuffer(BufferTarget.ArrayBuffer, vbo);

			//頂点の位置、法線、テクスチャ情報の場所を指定
			GL.VertexPointer(3, VertexPointerType.Float, Vertex.Size, 0);
			GL.NormalPointer(NormalPointerType.Float, Vertex.Size, Vector3.SizeInBytes);
			GL.TexCoordPointer(2, TexCoordPointerType.Float, Vertex.Size, Vector3.SizeInBytes * 2);

			GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
			GL.BindVertexArray(0);


			//Textureの許可
			GL.Enable(EnableCap.Texture2D);

			//テクスチャ用バッファの生成
			texture = GL.GenTexture();

			//テクスチャ用バッファのひもづけ
			GL.BindTexture(TextureTarget.Texture2D, texture);

			//テクスチャの設定
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);

			Bitmap file = new Bitmap("02-12_texture.png");

			//png画像の反転を直す
			file.RotateFlip(RotateFlipType.RotateNoneFlipY);

			//データ読み込み
			BitmapData data = file.LockBits(new Rectangle(0, 0, file.Width, file.Height), ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

			//テクスチャ用バッファに色情報を流し込む
			GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, data.Width, data.Height, 0, OpenTK.Graphics.OpenGL.PixelFormat.Bgra, PixelType.UnsignedByte, data.Scan0);

			GL.BindTexture(TextureTarget.Texture2D, 0);
		}

		//ウィンドウの終了時に実行される。
		protected override void OnUnload(EventArgs e)
		{
			base.OnUnload(e);

			GL.DeleteBuffers(1, ref vbo);			//バッファを1コ削除
			GL.DeleteBuffers(1, ref ibo);			//バッファを1コ削除
			GL.DeleteVertexArrays(1, ref vao);		//VAOを1コ削除

			GL.DisableClientState(ArrayCap.VertexArray);	//VertexArrayを無効化
			GL.DisableClientState(ArrayCap.NormalArray);	//NormalArrayを無効化
			GL.DisableClientState(ArrayCap.ColorArray);		//ColorArrayを無効化

			GL.DeleteTexture(texture);	//使用したテクスチャを削除
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
			GL.Material(MaterialFace.Front, MaterialParameter.Diffuse, materialDiffuse);
			GL.Material(MaterialFace.Front, MaterialParameter.Specular, materialSpecular);
			GL.Material(MaterialFace.Front, MaterialParameter.Shininess, materialShininess);

			GL.BindTexture(TextureTarget.Texture2D, texture);

			//サイコロを描画
			GL.BindVertexArray(vao);
			GL.BindBuffer(BufferTarget.ElementArrayBuffer, ibo);
			GL.DrawElements(BeginMode.Triangles, indices.Length, DrawElementsType.UnsignedInt, 0);
			GL.BindVertexArray(0);

			SwapBuffers();
		}

		void InitDice()
		{
			//本来は、ファイルから読み込んだり、ループ処理でデータを生成したりすべき
			vertices = new Vertex[]
			{
				new Vertex(new Vector3(-0.95f, 1.0f, -0.95f), Vector3.UnitY, new Vector2(0.25f, 0.5f)),
				new Vertex(new Vector3(0.95f, 1.0f, -0.95f), Vector3.UnitY, new Vector2(0.5f, 0.5f)),
				new Vertex(new Vector3(-0.95f, 1.0f, 0.95f), Vector3.UnitY, new Vector2(0.25f, 0.75f)),
				new Vertex(new Vector3(0.95f, 1.0f, 0.95f), Vector3.UnitY, new Vector2(0.5f, 0.75f)),
				
				new Vertex(new Vector3(-0.95f, 0.95f, 1.0f), Vector3.UnitZ, new Vector2(0.25f, 0.75f)),
				new Vertex(new Vector3(0.95f, 0.95f, 1.0f), Vector3.UnitZ, new Vector2(0.5f, 0.75f)),
				new Vertex(new Vector3(-0.95f, -0.95f, 1.0f), Vector3.UnitZ, new Vector2(0.25f, 1.0f)),
				new Vertex(new Vector3(0.95f, -0.95f, 1.0f), Vector3.UnitZ, new Vector2(0.5f, 1.0f)),

				new Vertex(new Vector3(-0.95f, -1.0f, 0.95f), -Vector3.UnitY, new Vector2(0.25f, 0.0f)),
				new Vertex(new Vector3(0.95f, -1.0f, 0.95f), -Vector3.UnitY, new Vector2(0.5f, 0.0f)),
				new Vertex(new Vector3(-0.95f, -1.0f, -0.95f), -Vector3.UnitY, new Vector2(0.25f, 0.25f)),
				new Vertex(new Vector3(0.95f, -1.0f, -0.95f), -Vector3.UnitY, new Vector2(0.5f, 0.25f)),

				new Vertex(new Vector3(-0.95f, -0.95f, -1.0f), -Vector3.UnitZ, new Vector2(0.25f, 0.25f)),
				new Vertex(new Vector3(0.95f, -0.95f, -1.0f), -Vector3.UnitZ, new Vector2(0.5f, 0.25f)),
				new Vertex(new Vector3(-0.95f, 0.95f, -1.0f), -Vector3.UnitZ, new Vector2(0.25f, 0.5f)),
				new Vertex(new Vector3(0.95f, 0.95f, -1.0f), -Vector3.UnitZ, new Vector2(0.5f, 0.5f)),

				new Vertex(new Vector3(1.0f, 0.95f, -0.95f), Vector3.UnitX, new Vector2(0.5f, 0.5f)),
				new Vertex(new Vector3(1.0f, -0.95f, -0.95f), Vector3.UnitX, new Vector2(0.75f, 0.5f)),
				new Vertex(new Vector3(1.0f, 0.95f, 0.95f), Vector3.UnitX, new Vector2(0.5f, 0.75f)),
				new Vertex(new Vector3(1.0f, -0.95f, 0.95f), Vector3.UnitX, new Vector2(0.75f, 0.75f)),

				new Vertex(new Vector3(-1.0f, -0.95f, -0.95f), -Vector3.UnitX, new Vector2(0.0f, 0.5f)),
				new Vertex(new Vector3(-1.0f, 0.95f, -0.95f), -Vector3.UnitX, new Vector2(0.25f, 0.5f)),
				new Vertex(new Vector3(-1.0f, -0.95f, 0.95f), -Vector3.UnitX, new Vector2(0.0f, 0.75f)),
				new Vertex(new Vector3(-1.0f, 0.95f, 0.95f), -Vector3.UnitX, new Vector2(0.25f, 0.75f)),

				//テクスチャ表示の整合用
				new Vertex(new Vector3(-0.95f, -1.0f, 0.95f), -Vector3.UnitY, new Vector2(-0.25f, 1.0f)),
				new Vertex(new Vector3(0.95f, -1.0f, 0.95f), -Vector3.UnitY, new Vector2(1.0f, 1.0f)),
				new Vertex(new Vector3(-0.95f, -1.0f, -0.95f), -Vector3.UnitY, new Vector2(-0.25f, 1.25f)),
				new Vertex(new Vector3(0.95f, -1.0f, -0.95f), -Vector3.UnitY, new Vector2(1.0f, 1.25f)),
			};
			indices = new int[]
			{
				0, 1, 3,
				3, 2, 0,

				4, 5, 7,
				7, 6, 4,

				8, 9, 11,
				11, 10, 8,

				12, 13, 15,
				15, 14, 12,

				16, 17, 19,
				19, 18, 16,

				20, 21, 23,
				23, 22, 20,


				2, 3, 5,
				5, 4, 2,

				//6, 7, 9,
				//9, 8, 6,
				6, 7, 25,
				25, 24, 6,

				14, 15, 1,
				1, 0, 14,

				10, 11, 13,
				13, 12, 10,

				3, 1, 16,
				16, 18, 3,
				
				//11, 9, 19,
				//19, 17, 11,
				27, 25, 19,
				19, 17, 27,

				0, 2, 23,
				23, 21, 0,
				
				//8, 10, 20,
				//20, 22, 8,
				24, 26, 20,
				20, 22, 24,

				7, 5, 18,
				18, 19, 7,

				15, 13, 17,
				17, 16, 15,

				4, 6, 22,
				22, 23, 4,

				12, 14, 21,
				21, 20, 12,


				3, 18, 5,

				4, 23, 2,

				//7, 19, 9,
				7, 19, 25,

				15, 16, 1,

				//8, 22, 6,
				24, 22, 6,
				
				11, 17, 13,
				
				0, 21, 14,
				
				12, 20, 10
			};
		}
	}
}