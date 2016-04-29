﻿using System;
using System.Collections.Generic;
using System.Net;

namespace PubNubMessaging.Core
{
    #region "Channel callback"
    /*internal struct PubnubChannelCallbackKey
    {
        public string Channel;
        public bool isChannelGroup;
        public ResponseType Type;
    }*/

    internal class PubnubChannelCallback<T>
    {
        public Action<T> SuccessCallback;
        public Action<PubnubClientError> ErrorCallback;
        public Action<T> ConnectCallback;
        public Action<T> DisconnectCallback;
        public Action<T> WildcardPresenceCallback;

        public PubnubChannelCallback ()
        {
            SuccessCallback = null;
            ConnectCallback = null;
            DisconnectCallback = null;
            ErrorCallback = null;
            WildcardPresenceCallback = null;
        }
    }

    public enum CallbackType
    {
        Success,
        Connect,
        Error,
        Disconnect,
        Wildcard
    }
    #endregion

    internal static class PubnubCallbacks
    {
        /*internal static void CallCallback<T>(PubnubChannelCallbackKey callbackKey, SafeDictionary<PubnubChannelCallbackKey, object> channelCallbacks, 
            IJsonPluggableLibrary jsonPluggableLibrary, List<object> itemMessage)
        {
            PubnubChannelCallback<T> currentPubnubCallback = channelCallbacks[callbackKey] as PubnubChannelCallback<T>;
            if (currentPubnubCallback != null && currentPubnubCallback.SuccessCallback != null)
            {
                GoToCallback<T>(itemMessage, currentPubnubCallback.SuccessCallback, jsonPluggableLibrary);
            }
        }

        internal static void CallCallbackKnownType<T>(PubnubChannelCallbackKey callbackKey, SafeDictionary<PubnubChannelCallbackKey, object> channelCallbacks, 
            IJsonPluggableLibrary jsonPluggableLibrary, List<object> itemMessage)
        {
            PubnubChannelCallback<T> currentPubnubCallback = channelCallbacks[callbackKey] as PubnubChannelCallback<T>;
            if (currentPubnubCallback != null && currentPubnubCallback.SuccessCallback != null)
            {
                GoToCallback(itemMessage, currentPubnubCallback.SuccessCallback, jsonPluggableLibrary);
            }
        }*/

        internal static void SendCallbacks<T>(IJsonPluggableLibrary jsonPluggableLibrary, ChannelEntity channelEntity, 
            List<object> itemMessage, CallbackType callbackType, bool checkType)
        {
            if ((itemMessage != null) && (itemMessage.Count > 0)) {
                SendCallbackChannelEntity<T> (jsonPluggableLibrary, channelEntity, itemMessage, callbackType, checkType);
            } else {
                #if (ENABLE_PUBNUB_LOGGING)
                LoggingMethod.WriteToLog (string.Format ("DateTime {0}, channelEntities null", DateTime.Now.ToString ()), LoggingMethod.LevelInfo);
                #endif
            }
        }

        internal static void SendCallbackChannelEntity<T>(IJsonPluggableLibrary jsonPluggableLibrary, ChannelEntity channelEntity, 
            List<object> itemMessage, CallbackType callbackType, bool checkType)
        {
            #if (ENABLE_PUBNUB_LOGGING)
            LoggingMethod.WriteToLog (string.Format ("DateTime {0}, currentChannel: {1}", DateTime.Now.ToString (), 
                channelEntity.ChannelID.ChannelOrChannelGroupName), LoggingMethod.LevelInfo);
            LoggingMethod.WriteToLog (string.Format ("DateTime {0}, typeof(T): {1}, TypeParameterType: {2}", 
                DateTime.Now.ToString (), typeof(T).ToString (), channelEntity.ChannelParams.TypeParameterType), LoggingMethod.LevelInfo);

            #endif

            if (checkType) {
                if ((channelEntity.ChannelParams.TypeParameterType.Equals (typeof(string)))
                    || (channelEntity.ChannelParams.TypeParameterType.Equals (typeof(object)))) {
                    SendCallback<T> (jsonPluggableLibrary, channelEntity, itemMessage, callbackType);
                }
            } else {
                SendCallback<T> (jsonPluggableLibrary, channelEntity, itemMessage, callbackType);
            }
        }

        internal static void SendCallbacks<T>(IJsonPluggableLibrary jsonPluggableLibrary, List<ChannelEntity> channelEntities, 
            List<object> itemMessage, CallbackType callbackType, bool checkType)
        {

            if (channelEntities != null) {
                if ((itemMessage != null) && (itemMessage.Count > 0)) {
                    foreach (ChannelEntity channelEntity in channelEntities) {
                        SendCallbackChannelEntity<T> (jsonPluggableLibrary, channelEntity, itemMessage, callbackType, checkType);
                    }
                } else {
                    #if (ENABLE_PUBNUB_LOGGING)
                    LoggingMethod.WriteToLog (string.Format ("DateTime {0}, itemMessage null or count <0", DateTime.Now.ToString ()), LoggingMethod.LevelInfo);
                    #endif
                }
            } else {
                #if (ENABLE_PUBNUB_LOGGING)
                LoggingMethod.WriteToLog (string.Format ("DateTime {0}, channelEntities null", 
                    DateTime.Now.ToString ()), LoggingMethod.LevelInfo);
                #endif
            }
        }

        internal static void SendCallback<T>(IJsonPluggableLibrary jsonPluggableLibrary, ChannelEntity channelEntity, 
            List<object> itemMessage, CallbackType callbackType){
            PubnubChannelCallback<T> channelCallbacks = channelEntity.ChannelParams.Callbacks as PubnubChannelCallback<T>;
            if (channelCallbacks != null) {
                if (callbackType.Equals (CallbackType.Connect)) {
                    GoToCallback<T> (itemMessage, channelCallbacks.ConnectCallback, jsonPluggableLibrary);
                } else if (callbackType.Equals (CallbackType.Disconnect)) {
                    GoToCallback<T> (itemMessage, channelCallbacks.DisconnectCallback, jsonPluggableLibrary);
                } else if (callbackType.Equals (CallbackType.Success)) {
                    GoToCallback<T> (itemMessage, channelCallbacks.SuccessCallback, jsonPluggableLibrary);
                } else if (callbackType.Equals (CallbackType.Wildcard)) {
                    GoToCallback<T> (itemMessage, channelCallbacks.WildcardPresenceCallback, jsonPluggableLibrary);
                }
            } else {
                #if (ENABLE_PUBNUB_LOGGING)
                LoggingMethod.WriteToLog (string.Format ("DateTime {0}, channelCallbacks null", DateTime.Now.ToString ()), LoggingMethod.LevelInfo);
                #endif
            }
        }

        /*internal static void SendConnectCallback<T> (IJsonPluggableLibrary jsonPluggableLibrary, 
            List<object> connectResult, ResponseType type, ChannelEntity channelEntity){

            PubnubChannelCallback<T> channelCallbacks = channelEntity.ChannelParams.Callbacks as PubnubChannelCallback<T>;
            if (channelCallbacks != null) {
                GoToCallback<T> (connectResult, channelCallbacks.ConnectCallback, jsonPluggableLibrary);
            }
        }*/

        internal static void FireErrorCallbacksForAllChannels<T> (WebException webEx, RequestState<T> requestState, 
            PubnubErrorSeverity severity, bool callbackObjectType,  PubnubErrorFilter.Level errorLevel){

            foreach (ChannelEntity channelEntity in requestState.ChannelEntities) {
                PubnubClientError error = Helpers.CreatePubnubClientError<T> (webEx, requestState, channelEntity.ChannelID.ChannelOrChannelGroupName, 
                    severity);  
                FireErrorCallback<T> (channelEntity,
                    callbackObjectType, requestState.RespType, errorLevel, error);
                
            }
        }

        internal static void FireErrorCallbacksForAllChannels<T> (Exception ex, RequestState<T> requestState, 
            PubnubErrorSeverity severity, bool callbackObjectType, PubnubErrorCode errorType, 
            PubnubErrorFilter.Level errorLevel){

            foreach (ChannelEntity channelEntity in requestState.ChannelEntities) {
                PubnubClientError error = Helpers.CreatePubnubClientError<T> (ex, requestState, channelEntity.ChannelID.ChannelOrChannelGroupName, 
                    severity);  
                FireErrorCallback<T> (channelEntity,
                    callbackObjectType, requestState.RespType, errorLevel, error);
            }
        }

        internal static void FireErrorCallbacksForAllChannels<T> (Exception ex, List<ChannelEntity> channelEntities,
            PubnubErrorSeverity severity, bool callbackObjectType, PubnubErrorCode errorType, 
            ResponseType responseType, PubnubErrorFilter.Level errorLevel){

            foreach (ChannelEntity channelEntity in channelEntities) {
                PubnubClientError error = Helpers.CreatePubnubClientError<T> (ex, null, channelEntity.ChannelID.ChannelOrChannelGroupName, 
                    severity);  
                FireErrorCallback<T> (channelEntity,
                    callbackObjectType, responseType, errorLevel, error);
            }
        }

        internal static void FireErrorCallbacksForAllChannels<T> (string message, RequestState<T> requestState,
            PubnubErrorSeverity severity, bool callbackObjectType, PubnubErrorCode errorType, 
            ResponseType responseType, PubnubErrorFilter.Level errorLevel){

            foreach (ChannelEntity channelEntity in requestState.ChannelEntities) {
                PubnubClientError error = Helpers.CreatePubnubClientError<T> (message, requestState, channelEntity.ChannelID.ChannelOrChannelGroupName, 
                    severity);  
                FireErrorCallback<T> (channelEntity,
                    callbackObjectType, responseType, errorLevel, error);
            }
        }

        internal static void FireErrorCallback<T> (ChannelEntity channelEntity, bool callbackObjectType, 
            ResponseType responseType, PubnubErrorFilter.Level errorLevel, 
            PubnubClientError error){

            PubnubChannelCallback<T> channelCallback = channelEntity.ChannelParams.Callbacks as PubnubChannelCallback<T>;
            if (channelCallback != null) {
                GoToCallback (errorLevel,  error, channelCallback.ConnectCallback);
            }

        }

        /*internal static PubnubChannelCallbackKey GetPubnubChannelCallbackKey(string activeChannel, ResponseType responseType, bool isChannelGroup){
            PubnubChannelCallbackKey callbackKey = new PubnubChannelCallbackKey ();
            callbackKey.Channel = activeChannel;
            callbackKey.isChannelGroup = isChannelGroup;
            callbackKey.Type = responseType;
            return callbackKey;
        }

        internal static PubnubChannelCallback<T> GetPubnubChannelCallback<T>(Action<T> userCallback, Action<T> connectCallback, 
            Action<PubnubClientError> errorCallback
        ){
            PubnubChannelCallback<T> pubnubChannelCallbacks = new PubnubChannelCallback<T> ();
            pubnubChannelCallbacks.SuccessCallback = userCallback;
            pubnubChannelCallbacks.ConnectCallback = connectCallback;
            pubnubChannelCallbacks.ErrorCallback = errorCallback;
            return pubnubChannelCallbacks;
        }*/

        internal static PubnubChannelCallback<T> GetPubnubChannelCallback<T>(Action<T> userCallback, Action<T> connectCallback, 
            Action<PubnubClientError> errorCallback, Action<T> disconnectCallback, Action<T> wildcardPresenceCallback
        ){
            PubnubChannelCallback<T> pubnubChannelCallbacks = new PubnubChannelCallback<T> ();
            pubnubChannelCallbacks.SuccessCallback = userCallback;
            pubnubChannelCallbacks.ConnectCallback = connectCallback;
            pubnubChannelCallbacks.ErrorCallback = errorCallback;
            pubnubChannelCallbacks.DisconnectCallback = disconnectCallback;
            pubnubChannelCallbacks.WildcardPresenceCallback = wildcardPresenceCallback;
            return pubnubChannelCallbacks;
        }

        #region "Error Callbacks"

        internal static void CallErrorCallback<T>(WebException webEx, 
            RequestState<T> requestState, PubnubErrorSeverity severity,
            PubnubErrorFilter.Level errorLevel){

            PubnubClientError clientError = Helpers.CreatePubnubClientError (webEx, requestState, severity);

            foreach (ChannelEntity channelEntity in requestState.ChannelEntities) {
                PubnubChannelCallback<T> channelCallback = channelEntity.ChannelParams.Callbacks as PubnubChannelCallback<T>;
                if (channelCallback != null) {
                    GoToCallback (clientError, channelCallback.ErrorCallback, errorLevel);
                }
            }
        }

        internal static void CallErrorCallback<T>(Exception ex, 
            RequestState<T> requestState, PubnubErrorCode errorCode, PubnubErrorSeverity severity,
            PubnubErrorFilter.Level errorLevel){

            PubnubClientError clientError = Helpers.CreatePubnubClientError (ex, requestState, errorCode, severity);

            GoToCallback (clientError, errorCallback, errorLevel);
        }

        internal static void CallErrorCallback<T>(Exception ex, 
            List<ChannelEntity> channelEntities, PubnubErrorCode errorCode, PubnubErrorSeverity severity,
            PubnubErrorFilter.Level errorLevel){

            PubnubClientError clientError = Helpers.CreatePubnubClientError (ex, requestState, errorCode, severity);
            foreach (ChannelEntity ce in channelEntities) {
                GoToCallback (clientError, errorCallback, errorLevel);
            }
        }

        internal static void CallErrorCallback<T>(string message, 
            RequestState<T> requestState, PubnubErrorCode errorCode, PubnubErrorSeverity severity,
            PubnubErrorFilter.Level errorLevel){

            //request state can be null

            PubnubClientError clientError = Helpers.CreatePubnubClientError<T> (message, requestState, 
                errorCode, severity);

            GoToCallback (clientError, errorCallback, errorLevel);
        }

        internal static void CallErrorCallback<T>(string message, 
            Action<PubnubClientError> errorCallback, PubnubErrorCode errorCode, PubnubErrorSeverity severity,
            PubnubErrorFilter.Level errorLevel){

            //request state can be null

            PubnubClientError clientError = Helpers.CreatePubnubClientError<T> (message, requestState, 
                errorCode, severity);

            GoToCallback (clientError, errorCallback, errorLevel);
        }

        internal static void CallErrorCallback<T>(ChannelEntity channelEntity, string message,
            PubnubErrorCode errorCode, PubnubErrorSeverity severity,
            PubnubErrorFilter.Level errorLevel){

            PubnubClientError clientError = Helpers.CreatePubnubClientError<T> (message, null, 
                errorCode, severity);

            GoToCallback (clientError, errorCallback, errorLevel);
        }

        internal static void CallErrorCallback<T>(List<ChannelEntity> channelEntities, string message,
            PubnubErrorCode errorCode, PubnubErrorSeverity severity,
            PubnubErrorFilter.Level errorLevel){


            PubnubClientError clientError = Helpers.CreatePubnubClientError<T> (message, null, 
                errorCode, severity);
            foreach (ChannelEntity ce in channelEntities) {
                GoToCallback (clientError, errorCallback, errorLevel);
            }
        }

        private static void JsonResponseToCallback<T> (List<object> result, Action<T> callback, IJsonPluggableLibrary jsonPluggableLibrary)
        {
            string callbackJson = "";

            if (typeof(T) == typeof(string)) {
                callbackJson = jsonPluggableLibrary.SerializeToJsonString (result);

                Action<string> castCallback = callback as Action<string>;
                castCallback (callbackJson);
            }
        }

        private static void JsonResponseToCallback<T> (object result, Action<T> callback, IJsonPluggableLibrary jsonPluggableLibrary)
        {
            string callbackJson = "";

            if (typeof(T) == typeof(string)) {
                try {
                    callbackJson = jsonPluggableLibrary.SerializeToJsonString (result);
                    #if (ENABLE_PUBNUB_LOGGING)
                    LoggingMethod.WriteToLog (string.Format ("DateTime {0}, after _jsonPluggableLibrary.SerializeToJsonString {1}", DateTime.Now.ToString (), callbackJson), LoggingMethod.LevelInfo);
                    #endif
                    Action<string> castCallback = callback as Action<string>;
                    castCallback (callbackJson);
                } catch (Exception ex) {
                    #if (ENABLE_PUBNUB_LOGGING)
                    LoggingMethod.WriteToLog (string.Format ("DateTime {0}, JsonResponseToCallback = {1} ", DateTime.Now.ToString (), ex.ToString ()), LoggingMethod.LevelError);        
                    #endif
                }
            }
        }

        internal static void GoToCallback<T> (object result, Action<T> Callback, IJsonPluggableLibrary jsonPluggableLibrary)
        {
            if (Callback != null) {
                if (typeof(T) == typeof(string)) {
                    JsonResponseToCallback (result, Callback, jsonPluggableLibrary);
                } else {
                    Callback ((T)(object)result);
                }
            }
        }

        internal static void GoToCallback (object result, Action<string> Callback, IJsonPluggableLibrary jsonPluggableLibrary)
        {
            if (Callback != null) {
                JsonResponseToCallback (result, Callback, jsonPluggableLibrary);
            }
        }

        internal static void GoToCallback (object result, Action<object> Callback)
        {
            if (Callback != null) {
                Callback (result);
            }
        }

        internal static void GoToCallback (PubnubClientError error, Action<PubnubClientError> Callback, PubnubErrorFilter.Level errorLevel)
        {
            if (Callback != null && error != null) {
                if ((int)error.Severity <= (int)errorLevel) { //Checks whether the error serverity falls in the range of error filter level
                    //Do not send 107 = PubnubObjectDisposedException
                    //Do not send 105 = WebRequestCancelled
                    //Do not send 130 = PubnubClientMachineSleep
                    if (error.StatusCode != 107
                        && error.StatusCode != 105
                        && error.StatusCode != 130
                        && error.StatusCode != 4040) { //Error Code that should not go out
                        Callback (error);
                    }
                }
            }
        }


        #endregion
    }
}

