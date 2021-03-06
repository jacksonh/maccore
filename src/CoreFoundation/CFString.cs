//
// CFBase.cs: Contains base types
//
// Authors:
//    Miguel de Icaza (miguel@novell.com)
//
// The class can be either constructed from a string (from user code)
// or from a handle (from iphone-sharp.dll internal calls).  This
// delays the creation of the actual managed string until actually
// required
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.Runtime.InteropServices;

using MonoMac.ObjCRuntime;
using MonoMac.Foundation;

namespace MonoMac.CoreFoundation {

	public struct CFRange {
		public int Location, Length;
		
		public CFRange (int l, int len)
		{
			Location = l;
			Length = len;
		}
	}

	public static class CFObject {
		[DllImport (Constants.CoreFoundationLibrary, CharSet=CharSet.Unicode)]
		internal extern static IntPtr CFRelease (IntPtr obj);

		[DllImport (Constants.CoreFoundationLibrary, CharSet=CharSet.Unicode)]
		internal extern static IntPtr CFRetain (IntPtr obj);
	}
	
	public class CFString : INativeObject, IDisposable {
		internal IntPtr handle;
		internal string str;
		
		
		[DllImport (Constants.CoreFoundationLibrary, CharSet=CharSet.Unicode)]
		extern static IntPtr CFStringCreateWithCharacters (IntPtr allocator, string str, int count);

		[DllImport (Constants.CoreFoundationLibrary, CharSet=CharSet.Unicode)]
		extern static int CFStringGetLength (IntPtr handle);

		[DllImport (Constants.CoreFoundationLibrary, CharSet=CharSet.Unicode)]
		extern static IntPtr CFStringGetCharactersPtr (IntPtr handle);

		[DllImport (Constants.CoreFoundationLibrary, CharSet=CharSet.Unicode)]
		extern static IntPtr CFStringGetCharacters (IntPtr handle, CFRange range, IntPtr buffer);
		
		public CFString (string str)
		{
			if (str == null)
				throw new ArgumentNullException ("str");
			
			handle = CFStringCreateWithCharacters (IntPtr.Zero, str, str.Length);
			this.str = str;
		}

		~CFString ()
		{
			Dispose (false);
		}

		public IntPtr Handle {
			get {
				return handle;
			}
		}

		[DllImport (Constants.CoreTextLibrary, EntryPoint="CFStringGetTypeID")]
		public extern static int GetTypeID ();
		
		public void Dispose ()
		{
			Dispose (true);
			GC.SuppressFinalize (this);
		}

		public virtual void Dispose (bool disposing)
		{
			str = null;
			if (handle != IntPtr.Zero){
				CFObject.CFRelease (handle);
				handle = IntPtr.Zero;
			}
		}
		
		internal CFString (IntPtr handle)
			: this (handle, false)
		{
		}
		
		[Preserve (Conditional = true)]
		internal CFString (IntPtr handle, bool owns)
		{
			this.handle = handle;
			if (!owns)
				CFObject.CFRetain (handle);
		}

		internal static string FetchString (IntPtr handle)
		{
			if (handle == IntPtr.Zero)
				return null;
			
			string str;
			
			int l = CFStringGetLength (handle);
			IntPtr u = CFStringGetCharactersPtr (handle);
			IntPtr buffer = IntPtr.Zero;
			if (u == IntPtr.Zero){
				CFRange r = new CFRange (0, l);
				buffer = Marshal.AllocCoTaskMem (l * 2);
				CFStringGetCharacters (handle, r, buffer);
				u = buffer;
			}
			unsafe {
				str = new string ((char *) u, 0, l);
			}
			
			if (buffer != IntPtr.Zero)
				Marshal.FreeCoTaskMem (buffer);

			return str;
		}
		
		public static implicit operator string (CFString x)
		{
			if (x.str == null)
				x.str = FetchString (x.handle);
			
			return x.str;
		}

		public static implicit operator CFString (string s)
		{
			return new CFString (s);
		}

		public int Length {
			get {
				if (str != null)
					return str.Length;
				else
					return CFStringGetLength (handle);
			}
		}

		[DllImport (Constants.CoreFoundationLibrary, CharSet=CharSet.Unicode)]
		extern static char CFStringGetCharacterAtIndex (IntPtr handle, int p);
		
		public char this [int p] {
			get {
				if (str != null)
					return str [p];
				else
					return CFStringGetCharacterAtIndex (handle, p);
			}
		}
	}
}
