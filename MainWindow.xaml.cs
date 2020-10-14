using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Diagnostics;
using System.Windows.Forms;
using System.Windows.Media;
using System.Text;
using System.ComponentModel;

namespace GhostrunnerTrainer
{
	public partial class MainWindow : Window
	{
		globalKeyboardHook kbHook = new globalKeyboardHook();
		Timer updateTimer;
		Process game;
		public bool hooked = false;
		DeepPointer cheatManagerDP, capsuleDP, charMoveCompDP, playerControllerDP, playerCharacterDP, worldDP, gameModeDP;
		IntPtr xVelPtr, yVelPtr, zVelPtr, xPosPtr, yPosPtr, zPosPtr, vLookPtr, hLookPtr, ghostPtr, godPtr, noclipPtr, movePtr, clipmovePtr, airPtr, collisionPtr, sumPtr, injPtr;

		private void teleBtn_Click(object sender, RoutedEventArgs e)
		{
			Teleport();
		}

		private void saveBtn_Click(object sender, RoutedEventArgs e)
		{
			StorePosition();
		}

		private void noclipBtn_Click(object sender, RoutedEventArgs e)
		{
			ToggleNoclip();
		}

		private void godBtn_Click(object sender, RoutedEventArgs e)
		{
			ToggleGod();
		}

		private void ghostBtn_Click(object sender, RoutedEventArgs e)
		{
			ToggleGhost();
		}

		float xVel, yVel, zVel, xPos, yPos, zPos, vLook, hLook;
		bool ghost, god, noclip;
		int charLFS;
		float[] storedPos = new float[5] { 0f, 0f, 0f, 0f, 0f };


		public MainWindow()
		{
			InitializeComponent();

			kbHook.KeyDown += InputKeyDown;
			kbHook.KeyUp += InputKeyUp;
			kbHook.HookedKeys.Add(System.Windows.Forms.Keys.F1);
			kbHook.HookedKeys.Add(System.Windows.Forms.Keys.F2);
			kbHook.HookedKeys.Add(System.Windows.Forms.Keys.F3);
			kbHook.HookedKeys.Add(System.Windows.Forms.Keys.F5);
			kbHook.HookedKeys.Add(System.Windows.Forms.Keys.F6);

			updateTimer = new Timer
			{
				Interval = (16) // ~60 Hz
			};
			updateTimer.Tick += new EventHandler(Update);
			updateTimer.Start();
		}

		private void Update(object sender, EventArgs e)
		{
			if (game == null || game.HasExited)
			{
				game = null;
				hooked = false;
			}
			if (!hooked)
				hooked = Hook();
			if (!hooked)
				return;
			try
			{
				DerefPointers();
			}
			catch (Exception)
			{
				return;
			}

			game.ReadValue<float>(xPosPtr, out xPos);
			game.ReadValue<float>(yPosPtr, out yPos);
			game.ReadValue<float>(zPosPtr, out zPos);
			game.ReadValue<float>(xVelPtr, out xVel);
			game.ReadValue<float>(yVelPtr, out yVel);
			game.ReadValue<float>(zVelPtr, out zVel);
			double hVel = Math.Floor(Math.Sqrt(xVel * xVel + yVel * yVel)+0.5f)/100;
			game.ReadValue<int>(sumPtr, out charLFS);
			if (charLFS != globalKeyboardHook.dc)
				game.WriteBytes(sumPtr, BitConverter.GetBytes(globalKeyboardHook.dc));
			game.ReadValue<float>(vLookPtr, out vLook);
			game.ReadValue<float>(hLookPtr, out hLook);

			game.ReadValue<bool>(godPtr, out god);
			game.ReadValue<bool>(ghostPtr, out ghost);
			game.ReadValue<bool>(noclipPtr, out noclip);

			SetLabel(god, godLabel);
			SetLabel(ghost, ghostLabel);
			SetLabel(noclip, noclipLabel);

			positionBlock.Text = (xPos/100).ToString("0.00") + "\n" + (yPos/100).ToString("0.00") + "\n" + (zPos/100).ToString("0.00");
			speedBlock.Text = hVel.ToString("0.00") + " m/s";
		}

		private bool Hook()
		{
			List<Process> processList = Process.GetProcesses().ToList().FindAll(x => x.ProcessName.Contains("Ghostrunner-Win64-Shipping"));
			if (processList.Count == 0)
			{
				game = null;
				return false;
			}
			game = processList[0];

			if (game.HasExited)
				return false;

			try
			{
				int mainModuleSize = game.MainModule.ModuleMemorySize;
				SetPointersByModuleSize(mainModuleSize);
				return true;
			}
			catch (Win32Exception ex)
			{
				Console.WriteLine(ex.ErrorCode);
				return false;
			}
		}

		private void SetPointersByModuleSize(int moduleSize)
		{
			switch (moduleSize)
			{
				case 65024000:
					Debug.WriteLine("found demo2 v3");
					charMoveCompDP = new DeepPointer(0x037F59B0, 0x30, 0x280, 0x0);
					capsuleDP = new DeepPointer(0x037F59B0, 0x30, 0x130, 0x0);
					playerControllerDP = new DeepPointer(0x037F59B0, 0x30, 0xDD8, 0x0);
					cheatManagerDP = new DeepPointer(0x037F59D8, 0x0);
					playerCharacterDP = new DeepPointer(0x037F59B0, 0x30, 0x0);
					worldDP = new DeepPointer(0x037F6F28, 0x178, 0x0);
					gameModeDP = new DeepPointer(0x0397C958, 0x128, 0x0);
					break;
				default:
					updateTimer.Stop();
					System.Windows.Forms.MessageBox.Show("This game version is not supported.", "Unsupported Game Version");
					Environment.Exit(0);
					break;
			}
		}

		private void DerefPointers()
		{
			IntPtr cmpPtr;
			charMoveCompDP.DerefOffsets(game, out cmpPtr);
			xVelPtr = cmpPtr + 0xC4;
			yVelPtr = cmpPtr + 0xC8;
			zVelPtr = cmpPtr + 0xCC;
			movePtr = cmpPtr + 0x168;
			airPtr = cmpPtr + 0x388;
			clipmovePtr = cmpPtr + 0x199;

			IntPtr capsulePtr;
			capsuleDP.DerefOffsets(game, out capsulePtr);
			xPosPtr = capsulePtr + 0x1D0;
			yPosPtr = capsulePtr + 0x1D4;
			zPosPtr = capsulePtr + 0x1D8;

			IntPtr playerControllerPtr;
			playerControllerDP.DerefOffsets(game, out playerControllerPtr);
			vLookPtr = playerControllerPtr + 0x280;
			hLookPtr = playerControllerPtr + 0x284;

			IntPtr cheatManagerPtr;
			cheatManagerDP.DerefOffsets(game, out cheatManagerPtr);
			godPtr = cheatManagerPtr + 0x88;
			ghostPtr = cheatManagerPtr + 0x89;
			noclipPtr = cheatManagerPtr + 0x8A;

			IntPtr playerCharacterPtr;
			playerCharacterDP.DerefOffsets(game, out playerCharacterPtr);
			collisionPtr = playerCharacterPtr + 0x5C;

			IntPtr gameModePtr;
			gameModeDP.DerefOffsets(game, out gameModePtr);
			sumPtr = gameModePtr + 0x380;

			IntPtr worldPtr;
			worldDP.DerefOffsets(game, out worldPtr);
			injPtr = worldPtr + 0x284;
		}

		private void InputKeyDown(object sender, KeyEventArgs e)
		{
			switch (e.KeyCode)
			{
				case Keys.F1:
					ToggleGhost();
					break;
				case Keys.F2:
					ToggleGod();
					break;
				case Keys.F3:
					ToggleNoclip();
					break;
				case Keys.F5:
					StorePosition();
					break;
				case Keys.F6:
					Teleport();
					break;
				default:
					break;
			}
			e.Handled = true;
		}

		private void ToggleNoclip()
		{
			IncInj();
			byte[] byteToWrite = new byte[1];
			if (noclip)
			{
				byteToWrite[0] = 0x0;
				game.WriteBytes(collisionPtr, new byte[1] { 0x44 });
				game.WriteBytes(movePtr, new byte[1] { 0x01 });
				game.WriteBytes(airPtr, new byte[1] { 0x60 });
			}
			else
			{
				byteToWrite[0] = 0x1;
				game.WriteBytes(movePtr, new byte[1] { 0x05 });
				game.WriteBytes(airPtr, new byte[1] { 0x48 });
				game.WriteBytes(collisionPtr, new byte[1] { 0x40 });
				game.WriteBytes(clipmovePtr, BitConverter.GetBytes(0x00458CA0));
			}
			game.WriteBytes(noclipPtr, byteToWrite);
		}

		private void ToggleGod()
		{
			byte[] byteToWrite = new byte[1];
			if (god)
				byteToWrite[0] = 0x0;
			else
				byteToWrite[0] = 0x1;
			IncInj();
			game.WriteBytes(godPtr, byteToWrite);

		}

		private void ToggleGhost()
		{
			byte[] byteToWrite = new byte[1];
			if (ghost)
				byteToWrite[0] = 0x0;
			else
				byteToWrite[0] = 0x1;
			IncInj();
			game.WriteBytes(ghostPtr, byteToWrite);
		}

		private void Teleport()
		{
			IncInj();
			game.WriteBytes(xPosPtr, BitConverter.GetBytes(storedPos[0]));
			game.WriteBytes(yPosPtr, BitConverter.GetBytes(storedPos[1]));
			game.WriteBytes(zPosPtr, BitConverter.GetBytes(storedPos[2]));
			game.WriteBytes(vLookPtr, BitConverter.GetBytes(storedPos[3]));
			game.WriteBytes(hLookPtr, BitConverter.GetBytes(storedPos[4]));
		}

		private void IncInj()
		{
			int current;
			game.ReadValue<int>(injPtr, out current);
			game.WriteBytes(injPtr, BitConverter.GetBytes(current + 1));
		}

		private void SetLabel(bool state, System.Windows.Controls.Label label)
		{
			if (state)
			{
				label.Content = "ON";
				label.Foreground = Brushes.Green;
			}
			else
			{
				label.Content = "OFF";
				label.Foreground = Brushes.Red;
			}
		}

		private void StorePosition()
		{
			storedPos = new float[5] { xPos, yPos, zPos, vLook, hLook };
		}

		private void InputKeyUp(object sender, KeyEventArgs e)
		{
			e.Handled = true;
		}
	}
}
