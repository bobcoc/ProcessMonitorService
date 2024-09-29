# Installation Instructions

Follow these steps to install and run the `ProcessMonitorService` on your system.

## Prerequisites

- **.NET Framework 4.8** must be installed on the target machine.
- Administrative privileges are required to install and manage Windows services.

## Building the Service

1. **Open the Solution in Visual Studio**

   - Open the `ProcessMonitorService` solution in Visual Studio.

2. **Build the Solution**

   - Go to **Build** > **Build Solution** or press `Ctrl + Shift + B`.
   - Ensure that the solution builds without any errors.

## Installing the Service

1. **Open Developer Command Prompt as Administrator**

   - Click on **Start**, search for **Developer Command Prompt for Visual Studio**.
   - Right-click and select **Run as administrator**.

2. **Navigate to the Build Output Directory**

   - Use the `cd` command to navigate to the directory where the compiled service executable is located.

   ```batch
   cd <path-to-your-project>\bin\Release
   ```

   Replace `<path-to-your-project>` with the actual path to your project's directory, such as `C:\Projects\ProcessMonitorService`.

3. **Install the Service Using InstallUtil.exe**

   - Run the following command to install the service:

   ```batch
   InstallUtil.exe ProcessMonitorService.exe
   ```

   - `InstallUtil.exe` is typically located in:

     - For 64-bit systems:

       ```batch
       C:\Windows\Microsoft.NET\Framework64\v4.0.30319\InstallUtil.exe
       ```

     - For 32-bit systems:

       ```batch
       C:\Windows\Microsoft.NET\Framework\v4.0.30319\InstallUtil.exe
       ```

     - If `InstallUtil.exe` is not in your system's PATH, use the full path:

       ```batch
       "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\InstallUtil.exe" ProcessMonitorService.exe
       ```

4. **Start the Service**

   - After successful installation, start the service with the following command:

   ```batch
   net start ProcessMonitorService
   ```

## Verifying the Service Installation

1. **Check the Service Status**

   - Open **Services** management console:

     - Press `Win + R`, type `services.msc`, and press `Enter`.
     - Locate **Process Monitor Service** in the list and ensure it is running.

2. **Check the Event Log**

   - Open **Event Viewer**:

     - Press `Win + R`, type `eventvwr.msc`, and press `Enter`.
     - Navigate to **Windows Logs** > **Application**.
     - Look for entries from **ProcessMonitorService** to verify that the service is logging events correctly.

## Uninstalling the Service

If you need to uninstall the service, follow these steps:

1. **Stop the Service**

   ```batch
   net stop ProcessMonitorService
   ```

2. **Uninstall the Service Using InstallUtil.exe**

   ```batch
   InstallUtil.exe /u ProcessMonitorService.exe
   ```

## Notes

- Ensure that you run all commands in the **Developer Command Prompt** or **Command Prompt** with administrative privileges.
- The service must be installed and run with sufficient permissions to manage processes and system reboot.
