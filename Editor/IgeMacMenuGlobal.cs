using System;
using System.Collections;
using System.Runtime.InteropServices;

using System.Collections.Generic;

using System.Diagnostics;

namespace MonoDevelop.MacInterop
{
	internal delegate CarbonEventHandlerStatus EventDelegate (IntPtr callRef, IntPtr eventRef, IntPtr userData);
	internal delegate CarbonEventHandlerStatus AEHandlerDelegate (IntPtr inEvnt, IntPtr outEvt, uint refConst);

	internal static class Carbon
	{
		public const string CarbonLib = "/System/Library/Frameworks/Carbon.framework/Versions/Current/Carbon";

		[DllImport (CarbonLib)]
		public static extern IntPtr GetApplicationEventTarget ();

		[DllImport (CarbonLib)]
		static extern EventStatus InstallEventHandler (IntPtr target, EventDelegate handler, uint count,
			CarbonEventTypeSpec [] types, IntPtr user_data, out IntPtr handlerRef);

		[DllImport (CarbonLib)]
		public static extern EventStatus RemoveEventHandler (IntPtr handlerRef);

		public static IntPtr InstallEventHandler (IntPtr target, EventDelegate handler, CarbonEventTypeSpec [] types)
		{
			IntPtr handlerRef;
			CheckReturn (InstallEventHandler (target, handler, (uint)types.Length, types, IntPtr.Zero, out handlerRef));
			return handlerRef;
		}

		public static IntPtr InstallApplicationEventHandler (EventDelegate handler, CarbonEventTypeSpec type)
		{
			return InstallEventHandler (GetApplicationEventTarget (), handler, new CarbonEventTypeSpec[] { type });
		}

		public static void CheckReturn (EventStatus status)
		{
			int intStatus = (int) status;
			if (intStatus < 0)
				throw new EventStatusException (status);
		}
	}

	internal enum CarbonEventHandlerStatus //this is an OSStatus
	{
		Handled = 0,
		NotHandled = -9874,
		UserCancelled = -128,
	}

	internal enum CarbonEventClass : uint
	{
		Mouse = 1836021107, // 'mous'
		Keyboard = 1801812322, // 'keyb'
		TextInput = 1952807028, // 'text'
		Application = 1634758764, // 'appl'
		RemoteAppleEvent = 1701867619,  //'eppc' //remote apple event?
		Menu = 1835363957, // 'menu'
		Window = 2003398244, // 'wind'
		Control = 1668183148, // 'cntl'
		Command = 1668113523, // 'cmds'
		Tablet = 1952607348, // 'tblt'
		Volume = 1987013664, // 'vol '
		Appearance = 1634758765, // 'appm'
		Service = 1936028278, // 'serv'
		Toolbar = 1952604530, // 'tbar'
		ToolbarItem = 1952606580, // 'tbit'
		Accessibility = 1633903461, // 'acce'
		HIObject = 1751740258, // 'hiob'
		AppleEvent = 1634039412, // 'aevt'
		Internet = 1196773964, // 'GURL'
	}

	internal enum CarbonEventCommand : uint
	{
		Process = 1,
		UpdateStatus = 2,
	}

	internal enum CarbonEventMenu : uint
	{
		BeginTracking = 1,
		EndTracking = 2,
		ChangeTrackingMode = 3,
		Opening = 4,
		Closed = 5,
		TargetItem = 6,
		MatchKey = 7,
	}

	internal enum CarbonEventApple
	{
		OpenApplication = 1868656752, // 'oapp'
		ReopenApplication = 1918988400, //'rapp'
		OpenDocuments = 1868853091, // 'odoc'
		PrintDocuments = 188563030, // 'pdoc'
		OpenContents = 1868787566, // 'ocon'
		QuitApplication =  1903520116, // 'quit'
		ShowPreferences = 1886545254, // 'pref'
		ApplicationDied = 1868720500, // 'obit'
		GetUrl = 1196773964, // 'GURL'
	}

	[StructLayout(LayoutKind.Sequential, Pack = 2)]
	struct CarbonEventTypeSpec
	{
		public CarbonEventClass EventClass;
		public uint EventKind;

		public CarbonEventTypeSpec (CarbonEventClass eventClass, UInt32 eventKind)
		{
			this.EventClass = eventClass;
			this.EventKind = eventKind;
		}

		public CarbonEventTypeSpec (CarbonEventMenu kind) : this (CarbonEventClass.Menu, (uint) kind)
		{
		}

		public CarbonEventTypeSpec (CarbonEventCommand kind) : this (CarbonEventClass.Command, (uint) kind)
		{
		}


		public CarbonEventTypeSpec (CarbonEventApple kind) : this (CarbonEventClass.AppleEvent, (uint) kind)
		{
		}

		public static implicit operator CarbonEventTypeSpec (CarbonEventMenu kind)
		{
			return new CarbonEventTypeSpec (kind);
		}

		public static implicit operator CarbonEventTypeSpec (CarbonEventCommand kind)
		{
			return new CarbonEventTypeSpec (kind);
		}

		public static implicit operator CarbonEventTypeSpec (CarbonEventApple kind)
		{
			return new CarbonEventTypeSpec (kind);
		}
	}

	class EventStatusException : SystemException
	{
		public EventStatusException (EventStatus status)
		{
			StatusCode = status;
		}

		public EventStatus StatusCode {
			get; private set;
		}
	}

	enum EventStatus // this is an OSStatus
	{
		Ok = 0,

		//event manager
		EventAlreadyPostedErr = -9860,
		EventTargetBusyErr = -9861,
		EventClassInvalidErr = -9862,
		EventClassIncorrectErr = -9864,
		EventHandlerAlreadyInstalledErr = -9866,
		EventInternalErr = -9868,
		EventKindIncorrectErr = -9869,
		EventParameterNotFoundErr = -9870,
		EventNotHandledErr = -9874,
		EventLoopTimedOutErr = -9875,
		EventLoopQuitErr = -9876,
		EventNotInQueueErr = -9877,
		EventHotKeyExistsErr = -9878,
		EventHotKeyInvalidErr = -9879,
	}
}

namespace MonoDevelop.MacInterop
{
	public static class ApplicationEvents
	{
		static object lockObj = new object ();

		static EventHandler<ApplicationQuitEventArgs> quit;
		static IntPtr quitHandlerRef = IntPtr.Zero;

		public static event EventHandler<ApplicationQuitEventArgs> Quit {
			add {
				lock (lockObj) {
					quit += value;
					if (quitHandlerRef == IntPtr.Zero)
						quitHandlerRef = Carbon.InstallApplicationEventHandler (HandleQuit, CarbonEventApple.QuitApplication);
				}
			}
			remove {
				lock (lockObj) {
					quit -= value;
					if (quit == null && quitHandlerRef != IntPtr.Zero) {
						Carbon.RemoveEventHandler (quitHandlerRef);
						quitHandlerRef = IntPtr.Zero;
					}
				}
			}
		}

		static CarbonEventHandlerStatus HandleQuit (IntPtr callRef, IntPtr eventRef, IntPtr user_data)
		{
			var args = new ApplicationQuitEventArgs ();
			quit (null, args);
			return args.UserCancelled? CarbonEventHandlerStatus.UserCancelled : args.HandledStatus;
		}
	}

	public class ApplicationEventArgs : EventArgs
	{
		public bool Handled { get; set; }

		internal CarbonEventHandlerStatus HandledStatus {
			get {
				return Handled? CarbonEventHandlerStatus.Handled : CarbonEventHandlerStatus.NotHandled;
			}
		}
	}

	public class ApplicationQuitEventArgs : ApplicationEventArgs
	{
		public bool UserCancelled { get; set; }
	}
}

namespace IgeMacIntegration {

	public class IgeMacMenu {

		[DllImport("libigemacintegration.dylib")]
		static extern void ige_mac_menu_connect_window_key_handler (IntPtr window);

		public static void ConnectWindowKeyHandler (Gtk.Window window)
		{
			ige_mac_menu_connect_window_key_handler (window.Handle);
		}

		[DllImport("libigemacintegration.dylib")]
		static extern void ige_mac_menu_set_global_key_handler_enabled (bool enabled);

		public static bool GlobalKeyHandlerEnabled {
			set { 
				ige_mac_menu_set_global_key_handler_enabled (value);
			}
		}

		[DllImport("libigemacintegration.dylib")]
		static extern void ige_mac_menu_set_menu_bar(IntPtr menu_shell);

		public static Gtk.MenuShell MenuBar { 
			set {
				ige_mac_menu_set_menu_bar(value == null ? IntPtr.Zero : value.Handle);
			}
		}

		[DllImport("libigemacintegration.dylib")]
		static extern void ige_mac_menu_set_quit_menu_item(IntPtr quit_item);

		public static Gtk.MenuItem QuitMenuItem { 
			set {
				ige_mac_menu_set_quit_menu_item(value == null ? IntPtr.Zero : value.Handle);
			}
		}

		[DllImport("libigemacintegration.dylib")]
		static extern IntPtr ige_mac_menu_add_app_menu_group();

		public static IgeMacIntegration.IgeMacMenuGroup AddAppMenuGroup() {
			IntPtr raw_ret = ige_mac_menu_add_app_menu_group();
			IgeMacIntegration.IgeMacMenuGroup ret = raw_ret == IntPtr.Zero ? null : (IgeMacIntegration.IgeMacMenuGroup) GLib.Opaque.GetOpaque (raw_ret, typeof (IgeMacIntegration.IgeMacMenuGroup), false);
			return ret;
		}
	}

	public class IgeMacMenuGroup : GLib.Opaque {

		[DllImport("libigemacintegration.dylib")]
		static extern void ige_mac_menu_add_app_menu_item(IntPtr raw, IntPtr menu_item, IntPtr label);

		public void AddMenuItem(Gtk.MenuItem menu_item, string label) {
			IntPtr native_label = GLib.Marshaller.StringToPtrGStrdup (label);
			ige_mac_menu_add_app_menu_item(Handle, menu_item == null ? IntPtr.Zero : menu_item.Handle, native_label);
			GLib.Marshaller.Free (native_label);
		}

		public IgeMacMenuGroup(IntPtr raw) : base(raw) {}
	}
}
