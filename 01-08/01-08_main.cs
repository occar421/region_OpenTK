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

		//0:点光源 1:スポットライト
		Color4 lightAmbient;			//光源の環境光成分
		Color4 lightDiffuse;			//光源の拡散光成分
		Color4 lightSpecular;			//光源の鏡面光成分
		float lightConstantAttenuation;	//光源の一定減衰率
		float lightLinerAttenuation;	//光源の線形減衰率
		float lightQuadraticAttenuation;//光源の二次減衰率

		Vector4 light0Position;			//点光源の位置

		Vector4 light1Position;			//スポットライトの位置
		Vector4 light1Direction;		//スポットライトの方向
		float light1Cutoff;				//スポットライトの放射角度
		float light1Exponent;			//スポットライトの輝度分布

		Color4 materialAmbient;			//材質の環境光成分
		//Color4 materialDiffuse;		//材質の拡散光成分
		Color4 materialSpecular;		//材質の鏡面光成分
		float materialShininess;		//材質の鏡面光の鋭さ

		Model torus, sphere, plate;

		float angle;
		bool isPointLight;

		//800x600のウィンドウを作る。タイトルは「1-8:Lighting(2)」
		public Game()
			: base(800, 600, GraphicsMode.Default, "1-8:Lighting(2)")
		{
			lightAmbient = new Color4(0.2f, 0.2f, 0.2f, 1.0f);
			lightDiffuse = new Color4(0.7f, 0.7f, 0.7f, 1.0f);
			lightSpecular = new Color4(1.0f, 1.0f, 1.0f, 1.0f);
			lightConstantAttenuation = 1.0f;
			lightLinerAttenuation = 0.1f;
			lightQuadraticAttenuation = 0.1f;

			light0Position = new Vector4(0.0f, 1.5f, 0.0f, 1.0f);

			light1Position = new Vector4(0.0f, 3.0f, 0.0f, 1.0f);
			light1Direction = new Vector4(0.0f, -1.0f, 0.0f, 1.0f);
			light1Cutoff = 25.0f;
			light1Exponent = 10.0f;

			light1Direction = Vector4.Transform(light1Direction, Matrix4.CreateRotationZ(MathHelper.PiOver4 / 2.0f));

			materialAmbient = new Color4(0.2f, 0.2f, 0.2f, 1.0f);
			//materialDiffuse = new Color4(0.7f, 0.7f, 0.7f, 1.0f);
			materialSpecular = new Color4(0.6f, 0.6f, 0.6f, 1.0f);
			materialShininess = 51.4f;

			torus = new TorusModel(16, 32, 0.4, 1.0);
			torus.Initialize();

			sphere = new SphereModel(16, 16, 1.0f);
			sphere.Initialize();

			plate = new PlateModel(32, 32, 5.0f, -1.0f);
			plate.Initialize();

			angle = 0.0f;

			isPointLight = true;

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

			//点の大きさ
			GL.PointSize(10.0f);

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

			//Lキー押すとスポットライト、離すと点光源
			if (Keyboard[Key.L])
			{
				GL.Disable(EnableCap.Light0);
				GL.Enable(EnableCap.Light1);
				isPointLight = false;
			}
			else
			{
				GL.Enable(EnableCap.Light0);
				GL.Disable(EnableCap.Light1);
				isPointLight = true;
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
			if (isPointLight)
			{
				GL.Light(LightName.Light0, LightParameter.Position, light0Position);
				GL.Light(LightName.Light0, LightParameter.Ambient, lightAmbient);
				GL.Light(LightName.Light0, LightParameter.Diffuse, lightDiffuse);
				GL.Light(LightName.Light0, LightParameter.Specular, lightSpecular);
				GL.Light(LightName.Light0, LightParameter.ConstantAttenuation, lightConstantAttenuation);
				GL.Light(LightName.Light0, LightParameter.LinearAttenuation, lightLinerAttenuation);
				GL.Light(LightName.Light0, LightParameter.QuadraticAttenuation, lightQuadraticAttenuation);
			}
			else
			{
				GL.Light(LightName.Light1, LightParameter.Position, light1Position);
				GL.Light(LightName.Light1, LightParameter.Ambient, lightAmbient);
				GL.Light(LightName.Light1, LightParameter.Diffuse, lightDiffuse);
				GL.Light(LightName.Light1, LightParameter.Specular, lightSpecular);
				GL.Light(LightName.Light1, LightParameter.ConstantAttenuation, lightConstantAttenuation);
				GL.Light(LightName.Light1, LightParameter.LinearAttenuation, lightLinerAttenuation);
				GL.Light(LightName.Light1, LightParameter.QuadraticAttenuation, lightQuadraticAttenuation);
				GL.Light(LightName.Light1, LightParameter.SpotDirection, light1Direction);
				GL.Light(LightName.Light1, LightParameter.SpotCutoff, light1Cutoff);
				GL.Light(LightName.Light1, LightParameter.SpotExponent, light1Exponent);
			}

			//材質の指定
			GL.Material(MaterialFace.Front, MaterialParameter.Ambient, materialAmbient);
			//GL.Material(MaterialFace.Front, MaterialParameter.Diffuse, materialDiffuse);
			GL.Material(MaterialFace.Front, MaterialParameter.Specular, materialSpecular);
			GL.Material(MaterialFace.Front, MaterialParameter.Shininess, materialShininess);
			//GL.Material(MaterialFace.Front, MaterialParameter.Emission, Color4.Black);

			GL.MatrixMode(MatrixMode.Modelview);

			//モデル描画
			GL.PushMatrix();
			GL.Translate(-1.5f, 0.0f, 0.0f);
			GL.Rotate(angle, Vector3.UnitZ);

			torus.Draw();

			GL.PopMatrix();
			GL.PushMatrix();
			GL.Translate(1.5f, 0.0f, 0.0f);
			GL.Rotate(angle, Vector3.UnitY);

			sphere.Draw();

			GL.PopMatrix();

			plate.Draw();

			//スポットライトを回転させる
			angle += 1f;
			light1Direction = Vector4.Transform(light1Direction, Matrix4.CreateRotationY(0.01f));

			//光源の位置を示す赤い点を描画
			GL.Disable(EnableCap.Lighting);
			GL.Begin(BeginMode.Points);
			GL.Color4(Color4.Red);
			GL.Vertex4(isPointLight ? light0Position : light1Position);
			GL.End();
			GL.Enable(EnableCap.Lighting);

			SwapBuffers();
		}
	}
}