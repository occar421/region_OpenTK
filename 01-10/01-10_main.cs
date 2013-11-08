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

		Vector4 lightPosition;			//光源の位置
		Color4 lightAmbient;			//光源の環境光成分
		Color4 lightDiffuse;			//光源の拡散光成分
		Color4 lightSpecular;			//光源の鏡面光成分

		Color4 materialAmbient;			//材質の環境光成分
		//Color4 materialDiffuse;		//材質の拡散光成分
		Color4 materialSpecular;		//材質の鏡面光成分
		float materialShininess;		//材質の鏡面光の鋭さ

		float[] fogColor;				//フォグの色

		Model torus, sphere, plate;

		float angle;

		//800x600のウィンドウを作る。タイトルは「1-10:Fog」
		public Game()
			: base(800, 600, GraphicsMode.Default, "1-10:Fog")
		{
			lightPosition = new Vector4(0.0f, 1.5f, 0.0f, 0.0f);
			lightAmbient = new Color4(0.2f, 0.2f, 0.2f, 1.0f);
			lightDiffuse = new Color4(0.7f, 0.7f, 0.7f, 1.0f);
			lightSpecular = new Color4(1.0f, 1.0f, 1.0f, 1.0f);

			materialAmbient = new Color4(0.2f, 0.2f, 0.2f, 1.0f);
			//materialDiffuse = new Color4(0.7f, 0.7f, 0.7f, 1.0f);
			materialSpecular = new Color4(0.6f, 0.6f, 0.6f, 1.0f);
			materialShininess = 51.4f;

			fogColor = new float[] { 0.5f, 0.5f, 0.5f, 1.0f };

			torus = new TorusModel(16, 32, 0.4, 1.0);
			torus.Initialize(new Color4(0.0f, 0.0f, 1.0f, 1.0f));

			sphere = new SphereModel(16, 16, 1.0f);
			sphere.Initialize(new Color4(1.0f, 0.0f, 0.0f, 1.0f));

			plate = new PlateModel(32, 32, 5.0f, -1.0f);
			plate.Initialize(Color4.Black);

			angle = 0.0f;

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

			//フォグの設定
			GL.Enable(EnableCap.Fog);
			GL.Fog(FogParameter.FogColor, fogColor);
			GL.Fog(FogParameter.FogMode, (int)FogMode.Linear);
			GL.Hint(HintTarget.FogHint, HintMode.Nicest);
			GL.Fog(FogParameter.FogStart, 3.0f);
			GL.Fog(FogParameter.FogEnd, 30.0f);

			torus.CreateBuffers();
			sphere.CreateBuffers();
			plate.CreateBuffers();
		}

		//ウィンドウの終了時に実行される。
		protected override void OnUnload(EventArgs e)
		{
			base.OnUnload(e);

			torus.DeleteBuffers();
			sphere.DeleteBuffers();
			plate.DeleteBuffers();

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

			#region Fog

			//ZキーでFogMode.Linear
			if (Keyboard[Key.Z])
			{
				this.Title = "1-10:Fog(FogMode.Linear)";

				GL.Fog(FogParameter.FogMode, (int)FogMode.Linear);
				GL.Fog(FogParameter.FogStart, 3.0f);
				GL.Fog(FogParameter.FogEnd, 30.0f);
			}

			//XキーでFogMode.Exp
			if (Keyboard[Key.X])
			{
				this.Title = "1-10:Fog(FogMode.Exp)";

				GL.Fog(FogParameter.FogMode, (int)FogMode.Exp);
				GL.Fog(FogParameter.FogDensity, 0.1f);
			}

			//CキーでFogMode.Exp2
			if (Keyboard[Key.C])
			{
				this.Title = "1-10:Fog(FogMode.Exp2)";

				GL.Fog(FogParameter.FogMode, (int)FogMode.Exp2);
				GL.Fog(FogParameter.FogDensity, 0.1f);
			}

			#endregion

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
			//GL.Material(MaterialFace.Front, MaterialParameter.Emission, Color4.Black);

			GL.MatrixMode(MatrixMode.Modelview);

			plate.Draw();

			//モデル描画
			GL.PushMatrix();
			GL.Rotate(angle, Vector3.UnitY);
			GL.Translate(-7.5f, 0.0f, 0.0f);

			torus.Draw();

			GL.PopMatrix();
			GL.PushMatrix();
			GL.Rotate(angle, Vector3.UnitY);
			GL.Translate(7.5f, 0.0f, 0.0f);

			sphere.Draw();

			GL.PopMatrix();

			//スポットライトを回転させる
			angle += 1f;

			SwapBuffers();
		}
	}
}