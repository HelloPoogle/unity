using System;

using System.Collections.Generic;
using UnityEngine;

namespace PubNubAPI
{
    public class ListPushProvisionsRequestBuilder: PubNubNonSubBuilder<ListPushProvisionsRequestBuilder, PNPushListProvisionsResult>, IPubNubNonSubscribeBuilder<ListPushProvisionsRequestBuilder, PNPushListProvisionsResult>
    {      
        public ListPushProvisionsRequestBuilder(PubNubUnity pn):base(pn, PNOperationType.PNPushNotificationEnabledChannelsOperation){
        }

        private string DeviceIDForPush{ get; set;}

        public void DeviceId(string deviceId){
            DeviceIDForPush = deviceId;
        }

        public PNPushType PushType {get;set;}
  
        #region IPubNubBuilder implementation

        public void Async(Action<PNPushListProvisionsResult, PNStatus> callback)
        {
            this.Callback = callback;
            if (string.IsNullOrEmpty (DeviceIDForPush)) {
                PNStatus pnStatus = base.CreateErrorResponseFromMessage("DeviceId is empty", null, PNStatusCategory.PNBadRequestCategory);
                Callback(null, pnStatus);

                return;
            }

            if (PushType.Equals(PNPushType.None)) {
                Debug.Log("PNPushType not selected, using GCM");                
                PushType = PNPushType.GCM;
            }
            base.Async(this);
        }
        #endregion

        protected override void RunWebRequest(QueueManager qm){
            RequestState requestState = new RequestState ();
            requestState.OperationType = OperationType;
            
           /*  Uri request = BuildRequests.BuildGetChannelsPushRequest(
                PushType,
                DeviceIDForPush,
                this.PubNubInstance.PNConfig.UUID,
                this.PubNubInstance.PNConfig.Secure,
                this.PubNubInstance.PNConfig.Origin,
                this.PubNubInstance.PNConfig.AuthKey,
                this.PubNubInstance.PNConfig.SubscribeKey,
                this.PubNubInstance.Version
            ); */

             Uri request = BuildRequests.BuildGetChannelsPushRequest(
                PushType,
                DeviceIDForPush,
                ref this.PubNubInstance
            );

            base.RunWebRequest(qm, request, requestState, this.PubNubInstance.PNConfig.NonSubscribeTimeout, 0, this);
        }

        protected override void CreatePubNubResponse(object deSerializedResult, RequestState requestState){
            //["channel1", "channel2"] 
            PNPushListProvisionsResult pnPushListProvisionsResult = new PNPushListProvisionsResult();
            Dictionary<string, object> dictionary = deSerializedResult as Dictionary<string, object>;
            PNStatus pnStatus = new PNStatus();
            if(dictionary != null) {
                string message = Utility.ReadMessageFromResponseDictionary(dictionary, "message");
                if(Utility.CheckDictionaryForError(dictionary, "error")){
                    pnPushListProvisionsResult = null;
                    pnStatus = base.CreateErrorResponseFromMessage(message, requestState, PNStatusCategory.PNUnknownCategory);
                }
            } else if(dictionary==null) {
                object[] c = deSerializedResult as object[];
                
                if (c != null) {
                    pnPushListProvisionsResult.Channels = new List<string>();
                    foreach(string ch in c){
                        pnPushListProvisionsResult.Channels.Add(ch);
                    }
                }   
            } else {
                pnPushListProvisionsResult = null;
                pnStatus = base.CreateErrorResponseFromMessage("Response dictionary is null", requestState, PNStatusCategory.PNMalformedResponseCategory);
            }

            Callback(pnPushListProvisionsResult, pnStatus);
        }

        // protected override void CreateErrorResponse(Exception exception, bool showInCallback, bool level){
            
        // }        
        
    }
}
