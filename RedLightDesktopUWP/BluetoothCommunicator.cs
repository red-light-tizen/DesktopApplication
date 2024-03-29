﻿/*
 *  BluetoothComunicator.cs  
 *  
 *  Created on :Sep 15, 2019
 *  Author : Kasania
 */

using System;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.Rfcomm;
using Windows.Devices.Enumeration;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;
using Windows.UI.Xaml.Controls;
using UnicodeEncoding = Windows.Storage.Streams.UnicodeEncoding;

namespace RedLightDesktopUWP
{
    class BluetoothCommunicator
    {

        private Guid guid;

        private StreamSocket streamSocket = null;
        private DataWriter dataWriter = null;
        private RfcommDeviceService RfService = null;
        private BluetoothDevice bluetoothDevice;
        private DataRegister dataRegister;

        private Func<object> onDisconnect;
        private Func<object> onReconnect;
        private ListBox debugLog;
        private bool debugable;
        private bool isConnected;
        private bool isCanceled;

        private string lastDevID;

        public BluetoothCommunicator(Guid guid, ref DataRegister dataRegister)
        {
            //
            this.guid = guid;
            this.dataRegister = dataRegister;
        }

        public void AddDebugLog(ref ListBox listBox)
        {
            this.debugLog = listBox;
            debugable = true;
        }

        private void WriteDebug(String data)
        {
            if (debugable)
            {
                debugLog.Items.Add(data);
            }
        }

        public async void Connect(String devID)
        {
            // Perform device access checks before trying to get the device.
            // First, we check if consent has been explicitly denied by the user.
            lastDevID = devID;
            isCanceled = false;
            isConnected = false;
            WriteDebug("Check Connection");

            DeviceAccessStatus accessStatus = DeviceAccessInformation.CreateFromId(devID).CurrentStatus;
            if (accessStatus == DeviceAccessStatus.DeniedByUser)
            {
                return;
            }
            // If not, try to get the Bluetooth device
            try
            {
                bluetoothDevice = await BluetoothDevice.FromIdAsync(devID);
            }
            catch (Exception)
            {
                return;
            }
            // If we were unable to get a valid Bluetooth device object,
            // it's most likely because the user has specified that all unpaired devices
            // should not be interacted with.
            if (bluetoothDevice == null)
            {
                return;
            }
            WriteDebug("Sync Devices..");

            // This should return a list of uncached Bluetooth services (so if the server was not active when paired, it will still be detected by this call
            long start = DateTime.Now.Ticks;

            RfcommDeviceServicesResult rfcommServices = null;
            while(true)
            {
                

                rfcommServices = await bluetoothDevice.GetRfcommServicesForIdAsync(
                RfcommServiceId.FromUuid(guid), BluetoothCacheMode.Uncached);

                if (rfcommServices.Services.Count > 0)
                {
                    WriteDebug($"{rfcommServices.Error}.");
                    RfService = rfcommServices.Services[0];
                    break;

                }
                lock (this)
                {
                    long current = DateTime.Now.Ticks;
                    if (current - 10_00000000 > start)
                    {
                        break;
                    }
                }
                

            }

            if(RfService == null)
            {
                isCanceled = true;
                Disconnect();
                onDisconnect();
                return;
            }

            // Do various checks of the SDP record to make sure you are talking to a device that actually supports the Bluetooth Rfcomm Chat Service
            var attributes = await RfService.GetSdpRawAttributesAsync();
            if (!attributes.ContainsKey(0x100))
            {

                return;
            }
            var attributeReader = DataReader.FromBuffer(attributes[0x100]);
            var attributeType = attributeReader.ReadByte();
            if (attributeType != ((4 << 3) | 5))
            {

                return;
            }
            var serviceNameLength = attributeReader.ReadByte();

            // The Service Name attribute requires UTF-8 encoding.
            attributeReader.UnicodeEncoding = UnicodeEncoding.Utf8;
            
            


            //------------bind Stream------
            lock (this)
            {
                streamSocket = new StreamSocket();
            }
            try
            {
                await streamSocket.ConnectAsync(RfService.ConnectionHostName, RfService.ConnectionServiceName);

                WriteDebug($"{RfService.ConnectionHostName} : {RfService.Device.ConnectionStatus}");
                
                dataRegister.conditionText.Text = "Checking Connection...";
                
                dataWriter = new DataWriter(streamSocket.OutputStream);

                DataReader chatReader = new DataReader(streamSocket.InputStream);

                isConnected = true;

                ReceiveStringLoop(chatReader);
            }
            catch (Exception ex) when ((uint)ex.HResult == 0x80070490) // ERROR_ELEMENT_NOT_FOUND
            {
                return;
            }
            catch (Exception ex) when ((uint)ex.HResult == 0x80072740) // WSAEADDRINUSE
            {
                return;
            }
            catch
            {

            }
        }

        public void SetOnDisconnect(Func<object> p)
        {
            onDisconnect = p;
        }

        public void SetOnReconnect(Func<object> p)
        {
            onReconnect = p;
        }

        public async void SendMessage(String message)
        {
            try
            {
                if (message.Length != 0)
                {
                    dataWriter.WriteString(Convert.ToString(message.Length));
                    dataWriter.WriteString(message);
                    await dataWriter.StoreAsync();
                    WriteDebug($"Message Send Success. Message :  {message}");
                    

                }
            }
            catch (Exception ex) when ((uint)ex.HResult == 0x80072745)
            {

            }
            catch (Exception) { }
        }

        public async void ReceiveStringLoop(DataReader chatReader)
        {

            if (isCanceled)
            {
                WriteDebug($"Cancel Connection.");
                Disconnect();
                return;
            }
            string message = "";
            try
            {
                
                while (true)
                {
                    
                        await chatReader.LoadAsync(sizeof(byte));
                        byte character = chatReader.ReadByte();

                        if (character == 0)
                        {
                            break;
                        }
                        message = string.Concat(message, (char)character);               

                }
                
                dataRegister.UpdateData(message);
                WriteDebug($"Message :  {message}");
                ReceiveStringLoop(chatReader);
            }
            catch (Exception ex)
            {
                lock (this)
                {
                    if (streamSocket == null)
                    {
                        // Do not print anything here -  the user closed the socket.
                        if ((uint)ex.HResult == 0x80072745) { }
                        else if ((uint)ex.HResult == 0x800703E3) { }
                    }
                    else
                    {
                        WriteDebug(ex.StackTrace);
                        Disconnect();
                        if((uint)ex.HResult == 0x8000000b)
                        {
                            Connect(lastDevID);
                            onReconnect();
                        }
                    }
                }
            }
        }

        public void Disconnect()
        {
            if (!isConnected)
            {
                isCanceled = true;
                return;
            }
            if (dataWriter != null)
            {
                dataWriter.DetachStream();
                dataWriter = null;
            }


            if (RfService != null)
            {
                RfService.Dispose();
                RfService = null;
            }
            lock (this)
            {
                if (streamSocket != null)
                {
                    streamSocket.Dispose();
                    streamSocket = null;
                }
            }

            dataRegister.UpdateData("0000;000;00000000;4;000;0000;00000;0000;+00.00000;-000.00000;");


            WriteDebug("Disconnected.");

            onDisconnect();

        }

    }

}
