using Dissonance;
using Newtonsoft.Json;
using System;
using Unity.Netcode;
using UnityEngine.Events;
using Random = UnityEngine.Random;

namespace TerminalDesktopMod
{
    [Serializable]
    public class FlashDriveSaveModel
    {
        public int DecodeLevel { get; set; }
    }

    public class FlashDriveProp: PhysicsProp
    {
        public static UnityEvent<FlashDriveProp> FlashLoadedEvent = new UnityEvent<FlashDriveProp>();
        public UsbPort UsbPort { get; set; }
        private int _flashIndex;
        public int FlashIndex
        {
            get
            {
                if (_flashIndex == 0)
                    _flashIndex = Random.Range(-int.MaxValue, int.MaxValue);
                return _flashIndex;
            }
            private set => _flashIndex = value;
        }
        public NetworkVariable<int> DecodeLevel { get; set; } = new NetworkVariable<int>();

        public override int GetItemDataToSave()
        {
            var flashSaves = DesktopStorage.TerminalDesktopSaveModel.FlashSaves;
            if (flashSaves.TryGetValue(FlashIndex, out var _))
                flashSaves[FlashIndex] = GetSaveData();
            else
                DesktopStorage.TerminalDesktopSaveModel.FlashSaves.Add(FlashIndex, GetSaveData());
            
            return FlashIndex;
        }
        
        public override void LoadItemSaveData(int saveData)
        {
            FlashIndex = saveData;
            isInFactory = false;
            FlashLoadedEvent.Invoke(this);
            if (!DesktopStorage.TerminalDesktopSaveModel.FlashSaves.TryGetValue(FlashIndex, out var data))
                return;
            LoadData(data);
            Main.Log.LogInfo($"load flash {saveData}");
        }

        public override void GrabItem()
        {
            if (UsbPort is null)
                return;
            UsbPort.PulledFlash();
            UsbPort = null;
        }

        public virtual string GetSaveData()
        {
            var saveModel = new FlashDriveSaveModel()
            {
                DecodeLevel = DecodeLevel.Value,
            };
            return JsonConvert.SerializeObject(saveModel);
        }
        public virtual void LoadData(string data)
        {
            var saveModel = JsonConvert.DeserializeObject<FlashDriveSaveModel>(data);
            DecodeLevel.Value = saveModel.DecodeLevel;
        }

        public virtual void UpdateDecodeLevel(int value)
        {
            if (IsServer)
                DecodeLevel.Value = value;
        }

        public override void OnDestroy()
        {
            base.OnDestroy();
            if (UsbPort is null)
                return;
            UsbPort.PulledFlash();
            UsbPort = null;
        }

        public override void OnPlaceObject()
        {
            base.OnPlaceObject();

            if (transform.parent == null || !transform.parent.TryGetComponent(out NetworkObject possibleUsbPort))
            {
                Main.Log.LogInfo("Flash drive placed, but not in a USB port. Ignoring.");
                return;
            }

            UsbPort port = possibleUsbPort.GetComponent<UsbPort>();

            if (port != null)
            {
                // To avoid the rotation changing due to dropped item logic in base LC, we set the
                // parentObject here to be the USB port.
                // This will get cleared by the base LC code when the Flash drive is picked up.
                Main.Log.LogInfo("Flash drive placed in USB port, setting parent object to USB port transform.");
                parentObject = possibleUsbPort.transform;
                PlayDropSFX();
            }
        }
    }
}