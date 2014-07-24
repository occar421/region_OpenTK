using System;
using System.Drawing;
using System.Drawing.Imaging;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using OpenTK.Input;

/*
 * 参照設定 : OpenTK System System.Drawing
 */

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

		int[] textures;

		//800x600のウィンドウを作る。タイトルは「2-8:Combine」
		public Game()
			: base(800, 600, GraphicsMode.Default, "2-8:Combine")
		{

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

			textures = new int[3];

			VSync = VSyncMode.On;
		}

		//ウィンドウの起動時に実行される。
		protected override void OnLoad(EventArgs e)
		{
			base.OnLoad(e);

			GL.ClearColor(Color4.Gray);
			GL.Enable(EnableCap.DepthTest);

			//テクスチャ用バッファの生成
			GL.GenTextures(3, textures);

			for (int i = 0; i < 3; i++)
			{
				//テクスチャ用バッファのひもづけ
				GL.BindTexture(TextureTarget.Texture2D, textures[i]);

				//テクスチャの設定
				GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
				GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);

				Bitmap file = new Bitmap("02-08_picture[" + i + "].png");

				//png画像の反転を直す
				file.RotateFlip(RotateFlipType.RotateNoneFlipY);

				//データ読み込み
				BitmapData data = file.LockBits(new Rectangle(0, 0, file.Width, file.Height), ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

				//テクスチャ用バッファに色情報を流し込む
				GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, data.Width, data.Height, 0, OpenTK.Graphics.OpenGL.PixelFormat.Bgra, PixelType.UnsignedByte, data.Scan0);
			}

			GL.ActiveTexture(TextureUnit.Texture0);
			GL.BindTexture(TextureTarget.Texture2D, textures[0]);
			GL.Enable(EnableCap.Texture2D);

			GL.ActiveTexture(TextureUnit.Texture1);
			GL.BindTexture(TextureTarget.Texture2D, textures[1]);
			GL.Enable(EnableCap.Texture2D);

			GL.ActiveTexture(TextureUnit.Texture2);
			GL.BindTexture(TextureTarget.Texture2D, textures[2]);//dummy
			GL.Enable(EnableCap.Texture2D);

			//Combineの設定
			GL.ActiveTexture(TextureUnit.Texture0);
			GL.TexEnv(TextureEnvTarget.TextureEnv, TextureEnvParameter.TextureEnvMode, (int)TextureEnvMode.Replace);

			GL.ActiveTexture(TextureUnit.Texture1);
			GL.TexEnv(TextureEnvTarget.TextureEnv, TextureEnvParameter.TextureEnvMode, (int)TextureEnvMode.Combine);
			GL.TexEnv(TextureEnvTarget.TextureEnv, TextureEnvParameter.TextureEnvColor, new float[] { 1.0f, 1.0f, 1.0f, 0.6f });

			GL.TexEnv(TextureEnvTarget.TextureEnv, TextureEnvParameter.CombineRgb, (int)TextureEnvModeCombine.Interpolate);
			GL.TexEnv(TextureEnvTarget.TextureEnv, TextureEnvParameter.Source0Rgb, (int)TextureEnvModeSource.Previous);
			GL.TexEnv(TextureEnvTarget.TextureEnv, TextureEnvParameter.Src1Rgb, (int)TextureEnvModeSource.Texture);
			GL.TexEnv(TextureEnvTarget.TextureEnv, TextureEnvParameter.Src2Rgb, (int)TextureEnvModeSource.Constant);
			GL.TexEnv(TextureEnvTarget.TextureEnv, TextureEnvParameter.Operand0Rgb, (int)TextureEnvModeOperandRgb.SrcColor);
			GL.TexEnv(TextureEnvTarget.TextureEnv, TextureEnvParameter.Operand1Rgb, (int)TextureEnvModeOperandRgb.SrcColor);
			GL.TexEnv(TextureEnvTarget.TextureEnv, TextureEnvParameter.Operand2Rgb, (int)TextureEnvModeOperandRgb.SrcAlpha);

			GL.TexEnv(TextureEnvTarget.TextureEnv, TextureEnvParameter.CombineAlpha, (int)TextureEnvModeCombine.Modulate);
			GL.TexEnv(TextureEnvTarget.TextureEnv, TextureEnvParameter.Src0Alpha, (int)TextureEnvModeSource.Previous);
			GL.TexEnv(TextureEnvTarget.TextureEnv, TextureEnvParameter.Src1Alpha, (int)TextureEnvModeSource.Texture);
			GL.TexEnv(TextureEnvTarget.TextureEnv, TextureEnvParameter.Operand0Alpha, (int)TextureEnvModeOperandAlpha.SrcAlpha);
			GL.TexEnv(TextureEnvTarget.TextureEnv, TextureEnvParameter.Operand1Alpha, (int)TextureEnvModeOperandAlpha.SrcAlpha);

			GL.ActiveTexture(TextureUnit.Texture2);
			GL.TexEnv(TextureEnvTarget.TextureEnv, TextureEnvParameter.TextureEnvMode, (int)TextureEnvMode.Combine);
			
			GL.TexEnv(TextureEnvTarget.TextureEnv, TextureEnvParameter.CombineRgb, (int)TextureEnvModeCombine.Subtract);
			GL.TexEnv(TextureEnvTarget.TextureEnv, TextureEnvParameter.Source0Rgb, (int)TextureEnvModeSource.PrimaryColor);
			GL.TexEnv(TextureEnvTarget.TextureEnv, TextureEnvParameter.Src1Rgb, (int)TextureEnvModeSource.Previous);
			GL.TexEnv(TextureEnvTarget.TextureEnv, TextureEnvParameter.Operand0Rgb, (int)TextureEnvModeOperandRgb.SrcColor);
			GL.TexEnv(TextureEnvTarget.TextureEnv, TextureEnvParameter.Operand1Rgb, (int)TextureEnvModeOperandRgb.SrcColor);

			GL.TexEnv(TextureEnvTarget.TextureEnv, TextureEnvParameter.CombineAlpha, (int)TextureEnvModeCombine.Modulate);
			GL.TexEnv(TextureEnvTarget.TextureEnv, TextureEnvParameter.Src0Alpha, (int)TextureEnvModeSource.Previous);
			GL.TexEnv(TextureEnvTarget.TextureEnv, TextureEnvParameter.Src1Alpha, (int)TextureEnvModeSource.Texture);
			GL.TexEnv(TextureEnvTarget.TextureEnv, TextureEnvParameter.Operand0Alpha, (int)TextureEnvModeOperandAlpha.SrcAlpha);
			GL.TexEnv(TextureEnvTarget.TextureEnv, TextureEnvParameter.Operand1Alpha, (int)TextureEnvModeOperandAlpha.SrcAlpha);
			
			GL.ActiveTexture(TextureUnit.Texture0);
		}

		//ウィンドウの終了時に実行される。
		protected override void OnUnload(EventArgs e)
		{
			base.OnUnload(e);

			//使用したテクスチャを削除
			GL.DeleteTextures(3, textures);
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

			//メインのポリゴン表示
			GL.Color4(Color4.White);
			GL.Begin(BeginMode.Quads);

			GL.MultiTexCoord2(TextureUnit.Texture0, 1.0, 1.0);
			GL.MultiTexCoord2(TextureUnit.Texture1, 1.0, 1.0);
			GL.MultiTexCoord2(TextureUnit.Texture2, 1.0, 1.0);
			GL.Vertex3(1, 1, 0);

			GL.MultiTexCoord2(TextureUnit.Texture0, 0.0, 1.0);
			GL.MultiTexCoord2(TextureUnit.Texture1, 0.0, 1.0);
			GL.MultiTexCoord2(TextureUnit.Texture2, 0.0, 1.0);
			GL.Vertex3(-1, 1, 0);

			GL.MultiTexCoord2(TextureUnit.Texture0, 0.0, 0.0);
			GL.MultiTexCoord2(TextureUnit.Texture1, 0.0, 0.0);
			GL.MultiTexCoord2(TextureUnit.Texture2, 0.0, 0.0);
			GL.Vertex3(-1, -1, 0);

			GL.MultiTexCoord2(TextureUnit.Texture0, 1.0, 0.0);
			GL.MultiTexCoord2(TextureUnit.Texture1, 1.0, 0.0);
			GL.MultiTexCoord2(TextureUnit.Texture2, 1.0, 0.0);
			GL.Vertex3(1, -1, 0);

			GL.End();

			SwapBuffers();
		}
	}
}