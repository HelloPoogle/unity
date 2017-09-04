using System;

using System.Collections.Generic;
using UnityEngine;

namespace PubNubAPI
{
    public class ListPushProvisionsRequestBuilder: PubNubNonSubBuilder<ListPushProvisionsRequestBuilder, PNPushListProvisionsResult>, IPubNubNonSubscribeBuilder<ListPushProvisionsRequestBuilder, PNPushListProvisionsResult>
    {      
        public ListPushProvisionsRequestBuilder(PubNubUnity pn):base(pn){

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
            Debug.Log ("ListPushProvisionsRequestBuilder Async");
            if (string.IsNullOrEmpty (DeviceIDForPush)) {
                Debug.Log("DeviceId is empty");

                //TODO Send callback
                return;
            }

            if (PushType.Equals(PNPushType.None)) {
                Debug.Log("PNPushType not selected, using GCM");
                PushType = PNPushType.GCM;
                //TODO Send callback
                return;
            }
            base.Async(callback, PNOperationType.PNPushNotificationEnabledChannelsOperation, PNCurrentRequestType.NonSubscribe, this);
        }
        #endregion

        protected override void RunWebRequest(QueueManager qm){
            RequestState<PNPushListProvisionsResult> requestState = new RequestState<PNPushListProvisionsResult> ();
            requestState.RespType = PNOperationType.PNPushNotificationEnabledChannelsOperation;
            
            Uri request = BuildRequests.BuildGetChannelsPushRequest(
                PushType,
                DeviceIDForPush,
                this.PubNubInstance.PNConfig.UUID,
                this.PubNubInstance.PNConfig.Secure,
                this.PubNubInstance.PNConfig.Origin,
                this.PubNubInstance.PNConfig.AuthKey,
                this.PubNubInstance.PNConfig.SubscribeKey,
                this.PubNubInstance.Version
            );

            this.PubNubInstance.PNLog.WriteToLog(string.Format("Run PNPushListProvisionsResult {0}", request.OriginalString), PNLoggingMethod.LevelInfo);
            base.RunWebRequest(qm, request, requestState, this.PubNubInstance.PNConfig.NonSubscribeTimeout, 0, this);
        }

        protected override void CreatePubNubResponse(object deSerializedResult){
            //["channel1", "channel2"] 
            PNPushListProvisionsResult pnPushListProvisionsResult = new PNPushListProvisionsResult();
            Dictionary<string, object> dictionary = deSerializedResult as Dictionary<string, object>;
            PNStatus pnStatus = new PNStatus();
            if (dictionary!=null && dictionary.ContainsKey("error") && dictionary["error"].Equals(true)){
                pnPushListProvisionsResult = null;
                pnStatus.Error = true;
                //TODO create error data
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
                pnStatus.Error = true;
            }
            Callback(pnPushListProvisionsResult, pnStatus);
        }

        // protected override void CreateErrorResponse(Exception exception, bool showInCallback, bool level){
            
        // }        
        
    }
}

