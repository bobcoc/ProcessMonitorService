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
