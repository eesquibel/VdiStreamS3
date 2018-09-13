//*****************************************************************************
// Copyright © 2007, Steve Abraham
//
// All rights reserved.
//
// Redistribution and use in source and binary forms, with or without
// modification, are permitted provided that the following conditions are met:
//
// Redistributions of source code must retain the above copyright notice, this
// list of conditions and the following disclaimer. 
//
// Redistributions in binary form must reproduce the above copyright notice,
// this list of conditions and the following disclaimer in the documentation
// and/or other materials provided with the distribution. 
//
// Neither the name of the ORGANIZATION nor the names of its contributors may
// be used to endorse or promote products derived from this software without
// specific prior written permission. 
//
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS"
// AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
// IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE
// ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE
// LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR
// CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF
// SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS
// INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN
// CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE)
// ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE
// POSSIBILITY OF SUCH DAMAGE.
//*****************************************************************************

#pragma once
#define _WIN32_DCOM
#include "vdi.h"
#include "vdierror.h"
#include "vdiguid.h"

using namespace System;
using namespace System::Data;
using namespace System::Data::Odbc;
using namespace System::Globalization;
using namespace System::IO;
using namespace System::Runtime::InteropServices;
using namespace System::Threading;

namespace VdiDotNet {

	public ref class InfoMessageEventArgs : EventArgs
	{
	private: String^ _Message;
	public: InfoMessageEventArgs(String^ message)
			 {
				 if (String::IsNullOrEmpty(message))
				 {
					 message = String::Empty;
				 }
				 else
				 {
					 if (message->StartsWith("[Microsoft][ODBC SQL Server Driver][SQL Server]"))
					 {
						 message = message->Replace("[Microsoft][ODBC SQL Server Driver][SQL Server]", String::Empty);
					 }
				 }

				 this->_Message = message;
			 }
	private: ~InfoMessageEventArgs()
			 {
				 delete (this->_Message);
			 }
	public: property String^ Message
			{
				String^ get() { return this->_Message; }
			}
	};
	public ref class CommandIssuedEventArgs : EventArgs
	{
	private: String^ _Command;
	public: CommandIssuedEventArgs(String^ message)
			 {
				 this->_Command = message;
			 }
	private: ~CommandIssuedEventArgs()
			 {
				 delete (this->_Command);
			 }
	public: property String^ Command
			{
				String^ get() { return this->_Command; }
			}
	};

	
	public ref class VdiEngine
	{
		//Delegates & Events
		public: event EventHandler<InfoMessageEventArgs^>^ InfoMessageReceived;
		public: event EventHandler<CommandIssuedEventArgs^>^ CommandIssued;
		
		private: Void SqlServerConnection_InfoMessage(Object^ sender, OdbcInfoMessageEventArgs^ e)
				{
					VdiDotNet::InfoMessageEventArgs^  i = gcnew VdiDotNet::InfoMessageEventArgs(e->Message);
					InfoMessageReceived(this, i);
				}
		private: Void ThreadFunc(Object^ data)
		{
			//Create and configure an ODBC connection to the local SQL Server
			OdbcConnection^ SqlServerConnection = gcnew OdbcConnection("Driver={SQL Server};Server=(local);Trusted_Connection=Yes;");
			SqlServerConnection->InfoMessage += gcnew OdbcInfoMessageEventHandler(this, &VdiDotNet::VdiEngine::SqlServerConnection_InfoMessage);

			//Create and configure the command to be issued to SQL Server
			OdbcCommand^ SqlServerCommand = gcnew OdbcCommand(data->ToString(), SqlServerConnection);
			SqlServerCommand->CommandType = CommandType::Text;
			SqlServerCommand->CommandTimeout = 0;

			//Notify the user of the command issued
			CommandIssued(this, gcnew CommandIssuedEventArgs(data->ToString()));

			//Open the connection
			SqlServerConnection->Open();

			//Execute the command
			SqlServerCommand->ExecuteNonQuery();
		}


		private: static Void ExecuteDataTransfer (IClientVirtualDevice* vd, Stream^ s)
		{
			//Declare Local Variables
			VDC_Command *   cmd;
			DWORD           completionCode;
			DWORD           bytesTransferred;
			HRESULT         hr;

			while (SUCCEEDED(hr = vd->GetCommand(INFINITE, &cmd)))
			{
				array<System::Byte>^ arr = gcnew array<System::Byte>(cmd->size);
				bytesTransferred = 0;
				switch (cmd->commandCode)
				{
					case VDC_Read:
						//Read the specified number of bytes from the stream
						bytesTransferred = s->Read(arr, bytesTransferred, cmd->size - bytesTransferred);

						//Copy the stream bytes to the cmd object
						Marshal::Copy(arr, 0, (IntPtr)cmd->buffer, cmd->size);

						//Set the completion code
						if (bytesTransferred == cmd->size)
						{
							completionCode = ERROR_SUCCESS;
						}
						else
						{
							completionCode = ERROR_READ_FAULT;
						}
						break;

					case VDC_Write:
						//Copy the data from the cmd object to a CLR array
						Marshal::Copy((IntPtr)cmd->buffer, arr, 0, cmd->size);

						//Write the data to the stream
						s->Write(arr, 0, cmd->size);
				
						//Set the number of bytes transferred
						bytesTransferred = cmd->size;

						//Set the completion code
						completionCode = ERROR_SUCCESS;
						break;

					case VDC_Flush:
						//Flush the stream
						s->Flush();

						//Set the completion code
						completionCode = ERROR_SUCCESS;
						break;
		    
					case VDC_ClearError:
						//Set the completion code
						completionCode = ERROR_SUCCESS;
						break;

					default:
						//Set the completion code
						completionCode = ERROR_NOT_SUPPORTED;
						break;
				}

				//Complete the command
				hr = vd->CompleteCommand(cmd, completionCode, bytesTransferred, 0);
				if (FAILED(hr))
				{
					throw gcnew ApplicationException(String::Format(gcnew CultureInfo("en-US"), "CompleteCommand Failed. HRESULT: {0}", hr));
				}
			}

			//If the command is not closed gracefully, throw an exception
			if (hr != VD_E_CLOSE)
			{
				switch (hr)
				{
					case VD_E_TIMEOUT:
						throw gcnew ApplicationException("GetCommand timed out.");
						break;
					case VD_E_ABORT:
						throw gcnew ApplicationException("GetCommand was aborted.");
						break;
					default:
						throw gcnew ApplicationException(String::Format(gcnew CultureInfo("en-US"), "GetCommand Failed. HRESULT: {0}", hr));
						break;
				};
			}
		}

		public: Void ExecuteCommand(System::String^ command, Stream^ commandStream)
		{
			//Initialize COM
			HRESULT hr = CoInitializeEx(NULL, COINIT_MULTITHREADED);
			if (FAILED(hr))
			{
				throw gcnew ApplicationException(String::Format(gcnew CultureInfo("en-US"), "CoInitializeEx Failed HRESULT: {0}", hr));
			}

			//Get an interface to the virtual device set
			IClientVirtualDeviceSet2* vds = NULL;
			hr = CoCreateInstance(CLSID_MSSQL_ClientVirtualDeviceSet, NULL, CLSCTX_INPROC_SERVER, IID_IClientVirtualDeviceSet2, (void**)&vds);
			if (FAILED(hr))
			{
				throw gcnew ApplicationException(String::Format(gcnew CultureInfo("en-US"), "Unable to get an interface to the virtual device set.  Please check to make sure sqlvdi.dll is registered. HRESULT: {0}", hr));
			}

			//Configure the device configuration
			VDConfig config = {0};
			config.deviceCount = 1;	//The number of virtual devices to create

			//Create a name for the device using a GUID
			String^ DeviceName = System::Guid::NewGuid().ToString()->ToUpper(gcnew CultureInfo("en-US"));
			WCHAR wVdsName [37] = {0};
			Marshal::Copy(DeviceName->ToCharArray(), 0, (IntPtr)wVdsName, DeviceName->Length);

			//Create the virtual device set
			hr = vds->CreateEx (NULL, wVdsName, &config);
			if (FAILED(hr))
			{
				throw gcnew ApplicationException(String::Format(gcnew CultureInfo("en-US"), "Unable to create and configure the virtual device set. HRESULT: {0}", hr));
			}

			//Format the command
			command = String::Format(gcnew CultureInfo("en-US"), command, DeviceName);

			//Create and execute a new thread to execute the command
			Thread^ OdbcThread = gcnew Thread(gcnew ParameterizedThreadStart(this, &VdiDotNet::VdiEngine::ThreadFunc));
			OdbcThread->Start(command);

			//Configure the virtual device set
			hr = vds->GetConfiguration(INFINITE, &config);
			if (FAILED(hr))
			{
				switch (hr)
				{
					case VD_E_ABORT:
						throw gcnew ApplicationException("GetConfiguration was aborted.");
						break;
					case VD_E_TIMEOUT:
						throw gcnew ApplicationException("GetConfiguration timed out.");
						break;
					default:
						throw gcnew ApplicationException(String::Format(gcnew CultureInfo("en-US"), "Un unknown exception was thrown during GetConfiguration.  HRESULT: {0}", hr));
						break;
				};
			}

			//Open the one device on the device set
			IClientVirtualDevice* vd = NULL;
			hr = vds->OpenDevice(wVdsName, &vd);
			if (FAILED(hr))
			{
				throw gcnew ApplicationException(String::Format(gcnew CultureInfo("en-US"), "OpenDevice Failed.  HRESULT: {0}", hr));
			}

			//Execute the data transfer
			ExecuteDataTransfer(vd, commandStream);

			//Wait for the thread that issued the backup / restore command to SQL Server to complete.
			OdbcThread->Join();
		}


		public: VdiEngine()
		{
		}
	};
}