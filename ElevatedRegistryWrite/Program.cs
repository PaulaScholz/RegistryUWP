using System;
using System.Diagnostics;
using Microsoft.Win32;

namespace ElevatedRegistryWrite
{
    /// <summary>
    /// This is the Win32 application used to write to the registry.
    /// 
    /// This application is started by the RegistryReadAppService in response to a request by
    /// the UWP application.
    /// </summary>
    class Program
    {
        static int Main(string[] args)
        {

            try
            {
                // Open the base key for what we need, HKEY_LOCAL_MACHINE, with the 64 bit view for the process,
                // Only x64 will persist to the actual HKEY_LOCAL_MACHINE, x86 processes will manipulate
                // a virtualized user copy that does not persist to the actual machine hive.
                RegistryKey baseKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);

                // open the SubKey for the Startup Programs list, with write access. Requires Admin privilege
                // because it is in HKEY_LOCAL_MACHINE and we want to write to it.
                RegistryKey key = baseKey.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true);

                string[] names = key.GetValueNames();

                bool bNotepadPresent = false;

                foreach (string name in names)
                {
                    if(name == "notepad")
                    {
                        bNotepadPresent = true;
                        break;
                    }
                }

                if(bNotepadPresent)
                {
                    // remove notepad from the list. If x64, notepad will no longer start after reboot.
                    key.DeleteValue("notepad");
                }
                else
                {
                    // add notepad to the list. If persisted to the real registry (x64), notepad will start after reboot.
                    key.SetValue("notepad", @"C:\Windows\System32\notepad.exe");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(String.Format("In ElevatedRegistryWrite key.SetValue, exception={0}", ex.Message));

                // return codes: 0 = OK, 1 = ElevationDialogCancelled, 2 = Exception
                // return code 1 is not raised here, but by the dialog itself if user says "No"
                return 2;
            }

            return 0;
        }
    }
}
