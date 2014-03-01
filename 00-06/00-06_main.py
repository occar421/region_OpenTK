#
# Reference Path : OpenTK
#

import clr
clr.AddReference("OpenTK")

from System import *
from OpenTK import *
from OpenTK.Graphics import *
from OpenTK.Graphics.OpenGL import *
from OpenTK.Input import *

class Game(GameWindow):
	#800x600のウィンドウを作る。タイトルは「0-6:with IronPython」
	def __init__(self):
		super(Game, self).__init__()
		self.Width = 800
		self.Height = 600
		self.Title = "0-6:with IronPython"
		self.VSync = VSyncMode.On
	
	#ウィンドウの起動時に実行される
	def OnLoad(self, e):
		super(Game, self).OnLoad(e)

		GL.ClearColor(Color4.Black)
		GL.Enable(EnableCap.DepthTest)

	#ウィンドウのサイズが変更された場合に実行される
	def OnResize(self, e):
		super(Game, self).OnResize(e)

		GL.Viewport(self.ClientRectangle.X, self.ClientRectangle.Y, self.ClientRectangle.Width, self.ClientRectangle.Height)
		GL.MatrixMode(MatrixMode.Projection)
		projection = Matrix4.CreatePerspectiveFieldOfView(float(Math.PI / 4), float(self.Width) / float(self.Height), 1.0, 64.0)
		GL.LoadMatrix(projection)

	#画面更新で実行される
	def OnUpdateFrame(self, e):
		super(Game, self).OnUpdateFrame(e)

		#Escapeキーで終了
		if(self.Keyboard[Key.Escape]):
			self.Exit()

	#画面描画で実行される
	def OnRenderFrame(self, e):
		super(Game, self).OnRenderFrame(e)

		GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit)

		GL.MatrixMode(MatrixMode.Modelview)
		modelview = Matrix4.LookAt(Vector3.Zero, Vector3.UnitZ, Vector3.UnitY)
		GL.LoadMatrix(modelview)

		GL.Begin(BeginMode.Quads)

		GL.Color4(Color4.White)								#色名で指定
		GL.Vertex3(-1.0, 1.0, 4.0)
		GL.Color4(Array[float]([1.0, 0.0, 0.0, 1.0]))		#配列で指定
		GL.Vertex3(-1.0, -1.0, 4.0)
		GL.Color4(0.0, 1.0, 0.0, 1.0)						#4つの引数にfloat型で指定
		GL.Vertex3(1.0, -1.0, 4.0)
		GL.Color4(Byte(0), Byte(0), Byte(255), Byte(255))	#byte型で指定
		GL.Vertex3(1.0, 1.0, 4.0)

		GL.End()

		self.SwapBuffers()

if __name__ == '__main__':
	window = Game()
	window.Run()
	window.Dispose()