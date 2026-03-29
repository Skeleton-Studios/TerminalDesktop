using GameNetcodeStuff;
using Newtonsoft.Json;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;

namespace TerminalDesktopMod
{
    public class UsbPortSaveModel
    {
        public int FlashInUsbIndex { get; set; }
    }
    public class UsbPort : NetworkBehaviour
    {
        public static UnityEvent<UsbPort> UsbPortChangeEvent = new UnityEvent<UsbPort>();
        public InteractTrigger triggerScript;
        public NetworkVariable<int> FlashInUsbIndex { get; set; } = new NetworkVariable<int>();
        public NetworkVariable<int> PortId { get; set; } = new NetworkVariable<int>();
        public FlashDriveProp FlashInUsb { get; set; }
        private void Awake()
        {
            FlashDriveProp.FlashLoadedEvent.AddListener(LoadFlash);
        }

        public override void OnDestroy()
        {
            base.OnDestroy();
            FlashDriveProp.FlashLoadedEvent.RemoveListener(LoadFlash);
        }
        
        private void Start()
        {
            TerminalDesktopManager.Instance.UsbPorts.Add(this);
            var terminal = ReferencesStorage.Terminal;
            transform.SetParent(FindTerminalTransform(terminal.transform), false);
            transform.localRotation = Quaternion.Euler(180, 0, 180);
            transform.localPosition = new Vector3(-0.413f, -0.0728f, 0.7024f);
        }

        Transform FindTerminalTransform(Transform trans)
        {
            while(!trans.gameObject.TryGetComponent(out NetworkObject networkObject) && trans != null)
            {
                trans = trans.parent;
            }
            return trans;
        }
        
        protected virtual void FixedUpdate()
        {
            if (GameNetworkManager.Instance is null || GameNetworkManager.Instance.localPlayerController is null)
                return;
            triggerScript.interactable = IsHoldFlash();
        }
        
        protected virtual bool IsHoldFlash()
        {
            var player = GameNetworkManager.Instance.localPlayerController;
            if (!player.isHoldingObject)
                return false;
            if (player.currentlyHeldObjectServer.itemProperties != DesktopStorage.FlashDriveItem)
                return false;
            if (FlashInUsb is not null)
                return false;
            return true;
        }
        public virtual void InsertIntoUsb(PlayerControllerB playerWhoTriggered)
        {
            if (!playerWhoTriggered.isHoldingObject || playerWhoTriggered.currentlyHeldObjectServer is null)
                return;
            var flash = playerWhoTriggered.currentlyHeldObjectServer;
            playerWhoTriggered.DiscardHeldObject(placeObject: true, GetComponent<NetworkObject>());
            InsertIntoUsbServerRpc(flash);
        }
        [ServerRpc(RequireOwnership = false)]
        private void InsertIntoUsbServerRpc(NetworkBehaviourReference flashRef)
        {
            if (!flashRef.TryGet(out FlashDriveProp flash))
                return;
            FlashInUsbIndex.Value = flash.FlashIndex;
            InsertIntoUsbClientRpc(flash);
        }
        [ClientRpc]
        private void InsertIntoUsbClientRpc(NetworkBehaviourReference flashRef)
        {
            if (!flashRef.TryGet(out FlashDriveProp flash))
                return;
            FlashInUsb = flash;
            flash.UsbPort = this;
            flash.transform.localRotation = Quaternion.Euler(0, 0, 180);
            UsbPortChangeEvent.Invoke(this);
        }

        public virtual void PulledFlash()
        {
            PulledFlashServerRpc();
        }
        [ServerRpc(RequireOwnership = false)]
        private void PulledFlashServerRpc()
        {
            FlashInUsbIndex.Value = 0;
            PulledFlashClientRpc();
        }
        [ClientRpc]
        private void PulledFlashClientRpc()
        {
            FlashInUsb = null;
            UsbPortChangeEvent.Invoke(this);
        }
        public virtual void LoadFlash(FlashDriveProp flashDriveProp)
        {
            if (flashDriveProp.FlashIndex != FlashInUsbIndex.Value)
            {
                return;
            }
            FlashDriveProp.FlashLoadedEvent.RemoveListener(LoadFlash);

            Main.Log.LogInfo($"Init flash in usb with index {flashDriveProp.FlashIndex}");
            InitLoadedFlash(flashDriveProp);
        }

        private void InitLoadedFlash(FlashDriveProp flashDriveProp)
        {
            flashDriveProp.reachedFloorTarget = true;
            flashDriveProp.fallTime = 1;
            flashDriveProp.transform.SetParent(transform, worldPositionStays: true);
            flashDriveProp.transform.localPosition = Vector3.zero;
            flashDriveProp.transform.localRotation = Quaternion.Euler(0, 0, 180);
            flashDriveProp.hasHitGround = true;
            flashDriveProp.parentObject = transform;

            FlashInUsb = flashDriveProp;
            flashDriveProp.UsbPort = this;
            UsbPortChangeEvent.Invoke(this);
        }

        public virtual string GetSaveString()
        {
            var saveModel = new UsbPortSaveModel()
            {
                FlashInUsbIndex = FlashInUsbIndex.Value
            };
            return JsonConvert.SerializeObject(saveModel);
        }
        
        public virtual void LoadPortById(int id)
        {
            LoadPortByIdServerRpc(id);
        }
        [ServerRpc(RequireOwnership = false)]
        private void LoadPortByIdServerRpc(int id)
        {
            PortId.Value = id;
            LoadPort();
        }
        protected virtual void LoadPort()
        {
            if (!IsServer)
                return;
            if (!DesktopStorage.TerminalDesktopSaveModel.UsbPortsSaves.TryGetValue(PortId.Value, out var data))
                return;
            
            var saveModel = JsonConvert.DeserializeObject<UsbPortSaveModel>(data);
            FlashInUsbIndex.Value = saveModel.FlashInUsbIndex;
            Main.Log.LogInfo("load usb port");
        }
    }
}