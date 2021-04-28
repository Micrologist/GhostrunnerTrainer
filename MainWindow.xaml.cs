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
		DeepPointer cheatManagerDP, capsuleDP, charMoveCompDP, playerControllerDP, playerCharacterDP, worldDP, gameModeDP, worldSettingsDP;
		IntPtr xVelPtr, yVelPtr, zVelPtr, xPosPtr, yPosPtr, zPosPtr, vLookPtr, hLookPtr, ghostPtr, godPtr, noclipPtr, movePtr, clipmovePtr, airPtr, collisionPtr, sumPtr, injPtr, gameSpeedPtr;

		private void gameSpeedBtn_Click(object sender, RoutedEventArgs e)
		{
			ChangeGameSpeed();
		}

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

		float xVel, yVel, zVel, xPos, yPos, zPos, vLook, hLook, gameSpeed, prefGameSpeed;
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
			kbHook.HookedKeys.Add(System.Windows.Forms.Keys.F4);
			kbHook.HookedKeys.Add(System.Windows.Forms.Keys.F5);
			kbHook.HookedKeys.Add(System.Windows.Forms.Keys.F6);
			kbHook.HookedKeys.Add(System.Windows.Forms.Keys.F7);

			prefGameSpeed = 1.0f;

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
			game.ReadValue<float>(gameSpeedPtr, out gameSpeed);

			Debug.WriteLine(gameSpeed.ToString());
			SetLabel(god, godLabel);
			SetLabel(ghost, ghostLabel);
			SetLabel(noclip, noclipLabel);

			SetGameSpeed();

			gameSpeedLabel.Content = prefGameSpeed.ToString("0.0") + "x";
			
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
				case 78057472:
					Debug.WriteLine("found steam1");
					charMoveCompDP = new DeepPointer(0x042E16B8, 0x30, 0x288, 0x0);
					capsuleDP = new DeepPointer(0x042E16B8, 0x30, 0x130, 0x0);
					playerControllerDP = new DeepPointer(0x042E16B8, 0x30, 0xCC0, 0x0);
					cheatManagerDP = new DeepPointer(0x042DFED8, 0x0);
					playerCharacterDP = new DeepPointer(0x042E16B8, 0x30, 0x0);
					worldDP = new DeepPointer(0x042E1678, 0x1A8, 0x0);
					worldSettingsDP = new DeepPointer(0x042E1678, 0x1A8, 0x20, 0x240, 0x0);
					gameModeDP = new DeepPointer(0x0455C860, 0x128, 0x0);
					break;
				case 78086144:
					Debug.WriteLine("found steam3");
					charMoveCompDP = new DeepPointer(0x042E78F8, 0x30, 0x288, 0x0);
					capsuleDP = new DeepPointer(0x042E78F8, 0x30, 0x130, 0x0);
					playerControllerDP = new DeepPointer(0x042E78F8, 0x30, 0xCC0, 0x0);
					cheatManagerDP = new DeepPointer(0x042E6118, 0x0);
					playerCharacterDP = new DeepPointer(0x042E78F8, 0x30, 0x0);
					worldDP = new DeepPointer(0x042E78D0, 0x1A8, 0x0);
					worldSettingsDP = new DeepPointer(0x042E78D0, 0x1A8, 0x20, 0x240, 0x0);
					gameModeDP = new DeepPointer(0x04562C20, 0x128, 0x0);
					break;
				case 78376960:
					Debug.WriteLine("found steam5");
					charMoveCompDP = new DeepPointer(0x04328538, 0x30, 0x288, 0x0);
					capsuleDP = new DeepPointer(0x04328538, 0x30, 0x130, 0x0);
					playerControllerDP = new DeepPointer(0x04328538, 0x30, 0xE0, 0x0);
					cheatManagerDP = new DeepPointer(0x04326CE8, 0x0);
					playerCharacterDP = new DeepPointer(0x04328538, 0x30, 0x0);
					worldDP = new DeepPointer(0x04328548, 0x1A8, 0x0);
					worldSettingsDP = new DeepPointer(0x04328548, 0x1A8, 0x20, 0x240, 0x0);
					gameModeDP = new DeepPointer(0x045A3C20, 0x128, 0x0);
					break;
				case 78856192:
					Debug.WriteLine("found steam6");
					charMoveCompDP = new DeepPointer(0x0438BB50, 0x30, 0x288, 0x0);
					capsuleDP = new DeepPointer(0x0438BB50, 0x30, 0x130, 0x0);
					playerControllerDP = new DeepPointer(0x0438BB50, 0x30, 0xE0, 0x0);
					cheatManagerDP = new DeepPointer(0x0438BB70, 0x0);
					playerCharacterDP = new DeepPointer(0x0438BB50, 0x30, 0x0);
					worldDP = new DeepPointer(0x0438BB40, 0x1A8, 0x0);
					worldSettingsDP = new DeepPointer(0x0438BB40, 0x1A8, 0x20, 0x240, 0x0);
					gameModeDP = new DeepPointer(0x04609420, 0x128, 0x0);
					break;
				case 78036992:
					Debug.WriteLine("found gog1");
					charMoveCompDP = new DeepPointer(0x0430CC48, 0x30, 0x288, 0x0);
					capsuleDP = new DeepPointer(0x0430CC48, 0x30, 0x130, 0x0);
					playerControllerDP = new DeepPointer(0x0430CC48, 0x30, 0xCC0, 0x0);
					cheatManagerDP = new DeepPointer(0x0430B3F0, 0x0);
					playerCharacterDP = new DeepPointer(0x0430CC48, 0x30, 0x0);
					worldDP = new DeepPointer(0x0430CC10, 0x1A8, 0x0);
					worldSettingsDP = new DeepPointer(0x0430CC10, 0x1A8, 0x20, 0x240, 0x0);
					gameModeDP = new DeepPointer(0x04587F20, 0x128, 0x0);
					break;
				case 78065664:
					Debug.WriteLine("found gog3");
					charMoveCompDP = new DeepPointer(0x04312E98, 0x30, 0x288, 0x0);
					capsuleDP = new DeepPointer(0x04312E98, 0x30, 0x130, 0x0);
					playerControllerDP = new DeepPointer(0x04312E98, 0x30, 0xE0, 0x0);
					cheatManagerDP = new DeepPointer(0x04311630, 0x0);
					playerCharacterDP = new DeepPointer(0x04312E98, 0x30, 0x0);
					worldDP = new DeepPointer(0x04312E70, 0x1A8, 0x0);
					worldSettingsDP = new DeepPointer(0x04312E70, 0x1A8, 0x20, 0x240, 0x0);
					gameModeDP = new DeepPointer(0x0458E2E0, 0x128, 0x0);
					break;	
				case 78168064:
					Debug.WriteLine("found gog5");
					charMoveCompDP = new DeepPointer(0x04328538, 0x30, 0x288, 0x0);
					capsuleDP = new DeepPointer(0x04328538, 0x30, 0x130, 0x0);
					playerControllerDP = new DeepPointer(0x04328538, 0x30, 0xE0, 0x0);
					cheatManagerDP = new DeepPointer(0x04326CE8, 0x0);
					playerCharacterDP = new DeepPointer(0x04328538, 0x30, 0x0);
					worldDP = new DeepPointer(0x04328548, 0x1A8, 0x0);
					worldSettingsDP = new DeepPointer(0x04328548, 0x1A8, 0x20, 0x240, 0x0);
					gameModeDP = new DeepPointer(0x045A3C20, 0x128, 0x0);
					break;
				case 78622720:
					Debug.WriteLine("found gog6");
					charMoveCompDP = new DeepPointer(0x0438BB50, 0x30, 0x288, 0x0);
					capsuleDP = new DeepPointer(0x0438BB50, 0x30, 0x130, 0x0);
					playerControllerDP = new DeepPointer(0x0438BB50, 0x30, 0xE0, 0x0);
					cheatManagerDP = new DeepPointer(0x0438BB70, 0x0);
					playerCharacterDP = new DeepPointer(0x0438BB50, 0x30, 0x0);
					worldDP = new DeepPointer(0x0438BB40, 0x1A8, 0x0);
					worldSettingsDP = new DeepPointer(0x0438BB40, 0x1A8, 0x20, 0x240, 0x0);
					gameModeDP = new DeepPointer(0x04609420, 0x128, 0x0);
					break;
				case 77885440:
					Debug.WriteLine("found egs1");
					charMoveCompDP = new DeepPointer(0x042EA0D0, 0x30, 0x288, 0x0);
					capsuleDP = new DeepPointer(0x042EA0D0, 0x30, 0x130, 0x0);
					playerControllerDP = new DeepPointer(0x042EA0D0, 0x30, 0xCC0, 0x0);
					cheatManagerDP = new DeepPointer(0x042E88F8, 0x0);
					playerCharacterDP = new DeepPointer(0x042EA0D0, 0x30, 0x0);
					worldDP = new DeepPointer(0x042EA098, 0x1A8, 0x0);
					worldSettingsDP = new DeepPointer(0x042EA098, 0x1A8, 0x20, 0x240, 0x0);
					gameModeDP = new DeepPointer(0x04565320, 0x128, 0x0);
					break;
				case 77881344:
					Debug.WriteLine("found egs2");
					charMoveCompDP = new DeepPointer(0x042E90D0, 0x30, 0x288, 0x0);
					capsuleDP = new DeepPointer(0x042E90D0, 0x30, 0x130, 0x0);
					playerControllerDP = new DeepPointer(0x042E90D0, 0x30, 0xCC0, 0x0);
					cheatManagerDP = new DeepPointer(0x042E78F8, 0x0);
					playerCharacterDP = new DeepPointer(0x042E90D0, 0x30, 0x0);
					worldDP = new DeepPointer(0x042E9098, 0x1A8, 0x0);
					worldSettingsDP = new DeepPointer(0x042E9098, 0x1A8, 0x20, 0x240, 0x0);
					gameModeDP = new DeepPointer(0x04564320, 0x128, 0x0);
					break;
				case 77910016:
					Debug.WriteLine("found egs3");
					charMoveCompDP = new DeepPointer(0x042F0310, 0x30, 0x288, 0x0);
					capsuleDP = new DeepPointer(0x042F0310, 0x30, 0x130, 0x0);
					playerControllerDP = new DeepPointer(0x042F0310, 0x30, 0xCC0, 0x0);
					cheatManagerDP = new DeepPointer(0x042EEB38, 0x0);
					playerCharacterDP = new DeepPointer(0x042F0310, 0x30, 0x0);
					worldDP = new DeepPointer(0x042F02E8, 0x1A8, 0x0);
					worldSettingsDP = new DeepPointer(0x042F02E8, 0x1A8, 0x20, 0x240, 0x0);
					gameModeDP = new DeepPointer(0x0456B6A0, 0x128, 0x0);
					break;
				default:
					updateTimer.Stop();
					Console.WriteLine(moduleSize.ToString());
					System.Windows.Forms.MessageBox.Show("This game version ("+moduleSize.ToString()+") is not supported.", "Unsupported Game Version");
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
			vLookPtr = playerControllerPtr + 0x288;
			hLookPtr = playerControllerPtr + 0x28C;

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
			sumPtr = gameModePtr + 0x388;

			IntPtr worldPtr;
			worldDP.DerefOffsets(game, out worldPtr);
			injPtr = worldPtr + 0x28C;

			IntPtr worldSettingsPtr;
			worldSettingsDP.DerefOffsets(game, out worldSettingsPtr);
			gameSpeedPtr = worldSettingsPtr + 0x2E8;
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
				case Keys.F4:
					ChangeGameSpeed();
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

		private void ChangeGameSpeed()
		{
			switch (prefGameSpeed)
			{
				case 1.0f:
					prefGameSpeed = 2.0f;
					break;
				case 2.0f:
					prefGameSpeed = 4.0f;
					break;
				case 4.0f:
					prefGameSpeed = 0.5f;
					break;
				case 0.5f:
					prefGameSpeed = 1.0f;
					break;
				default:
					prefGameSpeed = 1.0f;
					break;
			}
		}

		private void SetGameSpeed()
		{
			if((gameSpeed == 1.0f || gameSpeed == 2.0f || gameSpeed == 4.0f || gameSpeed == 0.5f) && gameSpeed != prefGameSpeed)
				game.WriteBytes(gameSpeedPtr, BitConverter.GetBytes(prefGameSpeed));
		}
	}
}
