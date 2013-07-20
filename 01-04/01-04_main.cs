/*
 * 参照設定 : OpenTK System System.Drawing
 */

using System;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using OpenTK.Input;

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
		Color4 lightAmbient;		//環境光
		Color4 lightDiffuse;		//
		Color4 lightSpecular;

		Color4 materialAmbient;
		Color4 materialDiffuse;
		Color4 materialSpecular;
		float materialShininess;

		//800x600のウィンドウを作る。タイトルは「1-4:Lighting(1)」
		public Game()
			: base(800, 600, GraphicsMode.Default, "1-4:Lighting(1)")
		{
			lightPosition = new Vector4(200.0f, 150f, 500.0f, 0.0f);
			lightAmbient = new Color4(0.2f, 0.2f, 0.2f, 1.0f);
			lightDiffuse = new Color4(0.7f, 0.7f, 0.7f, 1.0f);
			lightSpecular = new Color4(1.0f, 1.0f, 1.0f, 1.0f);

			materialAmbient = new Color4(0.24725f, 0.1995f, 0.0225f, 1.0f);
			materialDiffuse = new Color4(0.75164f, 0.60648f, 0.22648f, 1.0f);
			materialSpecular = new Color4(0.628281f, 0.555802f, 0.366065f, 1.0f);
			materialShininess = 51.4f;

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

			GL.Light(LightName.Light0, LightParameter.Position, lightPosition);
			GL.Light(LightName.Light0, LightParameter.Ambient, lightAmbient);
			GL.Light(LightName.Light0, LightParameter.Diffuse, lightDiffuse);
			GL.Light(LightName.Light0, LightParameter.Specular, lightSpecular);

			GL.Material(MaterialFace.Front, MaterialParameter.Ambient, materialAmbient);
			GL.Material(MaterialFace.Front, MaterialParameter.Diffuse, materialDiffuse);
			GL.Material(MaterialFace.Front, MaterialParameter.Specular, materialSpecular);
			GL.Material(MaterialFace.Front, MaterialParameter.Shininess, materialShininess);

			GL.MatrixMode(MatrixMode.Modelview);
			GL.PushMatrix();					//現在の(Modelview)行列をスタックに
			GL.Translate(2 * Vector3.UnitX);	//X軸方向に2移動
			DrawCube();							//立方体を描画
			GL.PopMatrix();						//スタックから(Modelview)行列を取り出す
			GL.PushMatrix();					//現在の行列をスタックに
			GL.Translate(-2 * Vector3.UnitX);	//X軸方向に-2移動
			DrawSphere();						//球を描画
			GL.PopMatrix();						//スタックから(Modelview)行列を取り出す

			SwapBuffers();
		}

		//立方体を描画する
		private void DrawCube()
		{
			GL.Begin(BeginMode.Quads);

			GL.Normal3(1.0f, 0.0f, 0.0f);
			GL.Vertex3(1.0f, 1.0f, 1.0f);
			GL.Vertex3(1.0f, -1.0f, 1.0f);
			GL.Vertex3(1.0f, -1.0f, -1.0f);
			GL.Vertex3(1.0f, 1.0f, -1.0f);

			GL.Normal3(-1.0f, 0.0f, 0.0f);
			GL.Vertex3(-1.0f, 1.0f, 1.0f);
			GL.Vertex3(-1.0f, 1.0f, -1.0f);
			GL.Vertex3(-1.0f, -1.0f, -1.0f);
			GL.Vertex3(-1.0f, -1.0f, 1.0f);

			GL.Normal3(0.0f, 1.0f, 0.0f);
			GL.Vertex3(1.0f, 1.0f, 1.0f);
			GL.Vertex3(1.0f, 1.0f, -1.0f);
			GL.Vertex3(-1.0f, 1.0f, -1.0f);
			GL.Vertex3(-1.0f, 1.0f, 1.0f);

			GL.Normal3(0.0f, -1.0f, 0.0f);
			GL.Vertex3(1.0f, -1.0f, 1.0f);
			GL.Vertex3(-1.0f, -1.0f, 1.0f);
			GL.Vertex3(-1.0f, -1.0f, -1.0f);
			GL.Vertex3(1.0f, -1.0f, -1.0f);

			GL.Normal3(0.0f, 0.0f, 1.0f);
			GL.Vertex3(1.0f, 1.0f, 1.0f);
			GL.Vertex3(-1.0f, 1.0f, 1.0f);
			GL.Vertex3(-1.0f, -1.0f, 1.0f);
			GL.Vertex3(1.0f, -1.0f, 1.0f);

			GL.Normal3(0.0f, 0.0f, -1.0f);
			GL.Vertex3(1.0f, 1.0f, -1.0f);
			GL.Vertex3(1.0f, -1.0f, -1.0f);
			GL.Vertex3(-1.0f, -1.0f, -1.0f);
			GL.Vertex3(-1.0f, 1.0f, -1.0f);

			GL.End();
		}

		//球を描画する
		private void DrawSphere()
		{
			int slices = 16, stacks = 16;	//横と縦の分割数
			double r = 1.24;				//半径
			for (int i = 0; i < stacks; i++)
			{
				//輪切り上部
				double upper =  Math.PI / stacks * i;
				double upperHeight = Math.Cos(upper);
				double upperWidth = Math.Sin(upper);
				//輪切り下部
				double lower =  Math.PI / stacks * (i + 1);
				double lowerHeight = Math.Cos(lower);
				double lowerWidth = Math.Sin(lower);

				GL.Begin(BeginMode.QuadStrip);
				for (int j = 0; j <= slices; j++)
				{
					//輪切りの面を単位円としたときの座標
					double rotor = 2 * Math.PI / slices * j;
					double x = Math.Cos(rotor);
					double y = Math.Sin(rotor);

					GL.Normal3(x * lowerWidth, lowerHeight, y * lowerWidth);
					GL.Vertex3(r * x * lowerWidth, r * lowerHeight, r * y * lowerWidth);
					GL.Normal3(x * upperWidth, upperHeight, y * upperWidth);
					GL.Vertex3(r * x * upperWidth, r * upperHeight, r * y * upperWidth);
				}
				GL.End();
			}
		}
	}
}
