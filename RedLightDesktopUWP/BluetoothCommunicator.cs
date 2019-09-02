using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
        private ListBox listBox;

        public BluetoothCommunicator(Guid guid, ref ListBox listBox)
        {
            //
            this.guid = guid;
            this.listBox = listBox;
        }

        public async void Connect(String devID)
        {
            // Perform device access checks before trying to get the device.
            // First, we check if consent has been explicitly denied by the user.
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

            // This should return a list of uncached Bluetooth services (so if the server was not active when paired, it will still be detected by this call
            var rfcommServices = await bluetoothDevice.GetRfcommServicesForIdAsync(
                RfcommServiceId.FromUuid(guid), BluetoothCacheMode.Uncached);

            if (rfcommServices.Services.Count > 0)
            {
                RfService = rfcommServices.Services[0];
            }
            else
            {
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

                //SetChatUI(attributeReader.ReadString(serviceNameLength), bluetoothDevice.Name);
                listBox.Items.Add($"Connected : {devID}");
                dataWriter = new DataWriter(streamSocket.OutputStream);

                DataReader chatReader = new DataReader(streamSocket.InputStream);
                //ReceiveStringLoop(chatReader);
            }
            catch (Exception ex) when ((uint)ex.HResult == 0x80070490) // ERROR_ELEMENT_NOT_FOUND
            {
                return;
            }
            catch (Exception ex) when ((uint)ex.HResult == 0x80072740) // WSAEADDRINUSE
            {
                return;
            }


        }

        public async void SendMessage(String message)
        {
            try
            {
                if (message.Length != 0)
                {
                    dataWriter.WriteString(message);
                    await dataWriter.StoreAsync();

                }
            }
            catch (Exception ex) when ((uint)ex.HResult == 0x80072745)
            {

            }
        }

        public async void ReceiveStringLoop(DataReader chatReader)
        {
            try
            {
                uint size = await chatReader.LoadAsync(sizeof(uint));
                if (size < sizeof(uint))
                {
                    Disconnect();
                    return;
                }

                uint stringLength = chatReader.ReadUInt32();
                uint actualStringLength = await chatReader.LoadAsync(stringLength);
                if (actualStringLength != stringLength)
                {
                    // The underlying socket was closed before we were able to read the whole data
                    return;
                }

                //ConversationList.Items.Add("Received: " + chatReader.ReadString(stringLength));
                // data process / display here
                listBox.Items.Add($"Connected : {chatReader.ReadString(stringLength)}");

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
                            //rootPage.NotifyUser("Disconnect triggered by remote device", NotifyType.StatusMessage);
                        else if ((uint)ex.HResult == 0x800703E3) { }
                            //rootPage.NotifyUser("The I/O operation has been aborted because of either a thread exit or an application request.", NotifyType.StatusMessage);
                    }
                    else
                    {
                        Disconnect();
                    }
                }
            }
        }

        public void Disconnect()
        {
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
            listBox.Items.Add("Disconnected");

        }

    }

}
