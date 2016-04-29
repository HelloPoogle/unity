﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;

namespace PubNubMessaging.Core
{
    internal class MultiplexExceptionEventArgs<T> : EventArgs
    {
        internal List<ChannelEntity> channelEntities;
        internal bool reconnectMaxTried;
        internal bool resumeOnReconnect;
        internal ResponseType responseType;
    }

    public class ExceptionHandlers
    {
        private static EventHandler<EventArgs> multiplexException;
        public static event EventHandler<EventArgs> MultiplexException {
            add {
                if (multiplexException == null || !multiplexException.GetInvocationList ().Contains (value)) {
                    multiplexException += value;
                }
            }
            remove {
                multiplexException -= value;
            }
        }

        internal static void ResponseCallbackErrorOrTimeoutHandler<T> (CustomEventArgs<T> cea, RequestState<T> requestState,
            PubnubErrorFilter.Level errorLevel, IJsonPluggableLibrary jsonPluggableLibrary){

            WebException webEx = new WebException (cea.Message);

            if ((cea.Message.Contains ("NameResolutionFailure")
                || cea.Message.Contains ("ConnectFailure")
                || cea.Message.Contains ("ServerProtocolViolation")
                || cea.Message.Contains ("ProtocolError")
            )) {
                webEx = new WebException ("Network connnect error", WebExceptionStatus.ConnectFailure);

                PubnubCallbacks.CallErrorCallback<T> (cea.Message, requestState, 
                    PubnubErrorCode.NoInternetRetryConnect, PubnubErrorSeverity.Warn, errorLevel);

            } else if (cea.IsTimeout || Utility.CheckRequestTimeoutMessageInError(cea)) {
            } else if ((cea.Message.Contains ("403")) 
                || (cea.Message.Contains ("java.io.FileNotFoundException")) 
                || ((PubnubUnity.Version.Contains("UnityWeb")) && (cea.Message.Contains ("Failed downloading")))) {
                PubnubClientError error = new PubnubClientError (403, PubnubErrorSeverity.Critical, cea.Message, PubnubMessageSource.Server, 
                    requestState.Request, requestState.Response, cea.Message, requestState.ChannelEntities);
                PubnubCallbacks.GoToCallback (error, requestState.ErrorCallback, jsonPluggableLibrary);
            } else if (cea.Message.Contains ("500")) {
                PubnubClientError error = new PubnubClientError (500, PubnubErrorSeverity.Critical, cea.Message, PubnubMessageSource.Server, 
                    requestState.Request, requestState.Response, cea.Message, requestState.ChannelEntities);
                PubnubCallbacks.GoToCallback (error, requestState.ErrorCallback, jsonPluggableLibrary);
            } else if (cea.Message.Contains ("502")) {
                PubnubClientError error = new PubnubClientError (503, PubnubErrorSeverity.Critical, cea.Message, PubnubMessageSource.Server, 
                    requestState.Request, requestState.Response, cea.Message, requestState.ChannelEntities);
                PubnubCallbacks.GoToCallback (error, requestState.ErrorCallback, jsonPluggableLibrary);
            } else if (cea.Message.Contains ("503")) {
                PubnubClientError error = new PubnubClientError (503, PubnubErrorSeverity.Critical, cea.Message, PubnubMessageSource.Server, 
                    requestState.Request, requestState.Response, cea.Message, requestState.ChannelEntities);
                PubnubCallbacks.GoToCallback (error, requestState.ErrorCallback, jsonPluggableLibrary);
            } else if (cea.Message.Contains ("504")) {
                PubnubClientError error = new PubnubClientError (504, PubnubErrorSeverity.Critical, cea.Message, PubnubMessageSource.Server, 
                    requestState.Request, requestState.Response, cea.Message, requestState.ChannelEntities);
                PubnubCallbacks.GoToCallback (error, requestState.ErrorCallback, jsonPluggableLibrary);
            } else if (cea.Message.Contains ("414")) {
                PubnubClientError error = new PubnubClientError (414, PubnubErrorSeverity.Critical, cea.Message, PubnubMessageSource.Server, 
                    requestState.Request, requestState.Response, cea.Message, requestState.ChannelEntities);
                PubnubCallbacks.GoToCallback (error, requestState.ErrorCallback, jsonPluggableLibrary);
            } else {
                PubnubClientError error = new PubnubClientError (400, PubnubErrorSeverity.Critical, cea.Message, PubnubMessageSource.Server, 
                    requestState.Request, requestState.Response, cea.Message, requestState.ChannelEntities);
                PubnubCallbacks.GoToCallback (error, requestState.ErrorCallback, jsonPluggableLibrary);
            }
            ProcessResponseCallbackWebExceptionHandler<T> (webEx, requestState, errorLevel);
        }

        internal static void ResponseCallbackWebExceptionHandler<T> (CustomEventArgs<T> cea, RequestState<T> requestState, 
            WebException webEx,
            PubnubErrorFilter.Level errorLevel){
            if ((requestState!=null) && (requestState.ChannelEntities != null || requestState.RespType != ResponseType.Time)) {

                if (requestState.RespType == ResponseType.Subscribe
                    || requestState.RespType == ResponseType.Presence) {

                    if (webEx.Message.IndexOf ("The request was aborted: The request was canceled") == -1
                        || webEx.Message.IndexOf ("Machine suspend mode enabled. No request will be processed.") == -1) {

                        PubnubCallbacks.FireErrorCallbacksForAllChannels<T> (webEx, requestState, 
                            PubnubErrorSeverity.Warn, true, errorLevel);
                    }
                } else {
                    PubnubCallbacks.CallErrorCallback<T> (webEx, requestState,
                        PubnubErrorSeverity.Warn, errorLevel);
                }
            }
            ProcessResponseCallbackWebExceptionHandler<T> (webEx, requestState, errorLevel);
        }

        internal static void ResponseCallbackExceptionHandler<T> (CustomEventArgs<T> cea, RequestState<T> requestState, 
            Exception ex, PubnubErrorFilter.Level errorLevel){

            #if (ENABLE_PUBNUB_LOGGING)
            LoggingMethod.WriteToLog (string.Format ("DateTime {0}, Process Response Exception: = {1}", DateTime.Now.ToString (), ex.ToString ()), LoggingMethod.LevelError);
            #endif
            if (requestState.ChannelEntities != null) {

                if (requestState.RespType == ResponseType.Subscribe
                    || requestState.RespType == ResponseType.Presence) {

                    PubnubCallbacks.FireErrorCallbacksForAllChannels (ex, requestState, 
                        PubnubErrorSeverity.Warn, false, PubnubErrorCode.None, errorLevel);
                } else {

                    PubnubCallbacks.CallErrorCallback<T> (ex, requestState, 
                        PubnubErrorCode.None, PubnubErrorSeverity.Critical, errorLevel);
                }
            }
            ProcessResponseCallbackExceptionHandler<T> (ex, requestState, errorLevel);
        }

        internal static void ProcessResponseCallbackExceptionHandler<T> (Exception ex, RequestState<T> asynchRequestState, 
            PubnubErrorFilter.Level errorLevel)
        {
            #if (ENABLE_PUBNUB_LOGGING)
            LoggingMethod.WriteToLog (string.Format ("DateTime {0} Exception= {1} for URL: {2}", DateTime.Now.ToString (), ex.ToString (), asynchRequestState.Request.RequestUri.ToString ()), LoggingMethod.LevelInfo);
            #endif
            UrlRequestCommonExceptionHandler<T> (ex.Message, asynchRequestState, asynchRequestState.Timeout, 
                false, errorLevel);
        }

        internal static void ProcessResponseCallbackWebExceptionHandler<T> (WebException webEx, RequestState<T> asynchRequestState, 
             PubnubErrorFilter.Level errorLevel)
        {
            #if (ENABLE_PUBNUB_LOGGING)
            if (webEx.ToString ().Contains ("Aborted")) {
                LoggingMethod.WriteToLog (string.Format ("DateTime {0}, WebException: {1}", DateTime.Now.ToString (), webEx.ToString ()), LoggingMethod.LevelInfo);
            } else {
                LoggingMethod.WriteToLog (string.Format ("DateTime {0}, WebException: {1}", DateTime.Now.ToString (), webEx.ToString ()), LoggingMethod.LevelError);
            }
            #endif

            UrlRequestCommonExceptionHandler<T> (webEx.Message, asynchRequestState, asynchRequestState.Timeout,
                false, errorLevel);
        }

        static void FireMultiplexException<T>(bool resumeOnReconnect, RequestState<T> requestState)
        {
            #if (ENABLE_PUBNUB_LOGGING)
            LoggingMethod.WriteToLog(string.Format("DateTime {0}, UrlRequestCommonExceptionHandler for Subscribe/Presence", DateTime.Now.ToString()), LoggingMethod.LevelInfo);
            #endif
            MultiplexExceptionEventArgs<T> mea = new MultiplexExceptionEventArgs<T>();
            mea.channelEntities = requestState.ChannelEntities;
            mea.resumeOnReconnect = resumeOnReconnect;
            mea.reconnectMaxTried = false;
            mea.responseType = requestState.RespType;

            multiplexException.Raise(typeof(ExceptionHandlers), mea);
        }

        internal static void UrlRequestCommonExceptionHandler<T> (string message, RequestState<T> requestState,
            bool requestTimeout, bool resumeOnReconnect, PubnubErrorFilter.Level errorLevel)
        {
            /*UrlRequestCommonExceptionHandler<T> (message, requestState.RespType, requestState.Channels, requestTimeout, 
                requestState.UserCallback, requestState.ConnectCallback, requestState.ErrorCallback,
                resumeOnReconnect, errorLevel, requestState.ChannelGroups
            );
        }

        internal static void UrlRequestCommonExceptionHandler<T> (string message, RequestState<T> requestState, 
            bool requestTimeout, bool resumeOnReconnect, PubnubErrorFilter.Level errorLevel)
        {*/
            switch (requestState.RespType)
            {
                case ResponseType.Presence:
                case ResponseType.Subscribe:
                    FireMultiplexException<T>(resumeOnReconnect, requestState);
                    break;
                case ResponseType.GlobalHereNow:
                case ResponseType.Time:
                    CommonExceptionHandler<T>(requestState, message, requestTimeout, errorLevel);
                    break;
                case ResponseType.Leave:
                case ResponseType.PresenceHeartbeat:
                    break;
                case ResponseType.PushGet:
                case ResponseType.PushRegister:
                case ResponseType.PushRemove:
                case ResponseType.PushUnregister:
                    PushNotificationExceptionHandler<T>(requestState, requestTimeout, errorLevel);
                    break;
                case ResponseType.ChannelGroupAdd:
                case ResponseType.ChannelGroupRemove:
                case ResponseType.ChannelGroupGet:
                case ResponseType.ChannelGroupGrantAccess:
                case ResponseType.ChannelGroupAuditAccess:
                case ResponseType.ChannelGroupRevokeAccess:
                    ChannelGroupExceptionHandler<T>(requestState, requestTimeout, errorLevel);
                    break;
                default:
                    CommonExceptionHandler<T> (requestState, message, requestTimeout, errorLevel);
                    break;
                    
            }
        }

        internal static void PushNotificationExceptionHandler<T>(RequestState<T> requestState, bool requestTimeout, 
            PubnubErrorFilter.Level errorLevel )
        {
            if (requestTimeout)
            {
                string message = (requestTimeout) ? "Operation Timeout" : "Network connnect error";
                #if (ENABLE_PUBNUB_LOGGING)
                LoggingMethod.WriteToLog(string.Format("DateTime {0}, PushExceptionHandler response={1}", DateTime.Now.ToString(), message), LoggingMethod.LevelInfo);
                #endif
                PubnubCallbacks.CallErrorCallback <T>(message, requestState, 
                    PubnubErrorCode.PushNotificationTimeout, PubnubErrorSeverity.Critical, errorLevel);
            }
        }

        internal static void ChannelGroupExceptionHandler<T>(RequestState<T> requestState, bool requestTimeout, 
            PubnubErrorFilter.Level errorLevel)
        {
            if (requestTimeout)
            {
                string message = (requestTimeout) ? "Operation Timeout" : "Network connnect error";
                //string channelAndChannelGroupMessage = string.Format("channels: {0}, channelGroups: {1}", channel, channelGroup);

                #if (ENABLE_PUBNUB_LOGGING)
                LoggingMethod.WriteToLog(string.Format("DateTime {0}, ChannelGroupExceptionHandler response={1}, {2}", 
                    DateTime.Now.ToString(), message, Helpers.GetNamesFromChannelEntities(requestState.ChannelEntities)), LoggingMethod.LevelInfo);
                #endif
                PubnubCallbacks.CallErrorCallback <T>(message, requestState, 
                    PubnubErrorCode.ChannelGroupTimeout, PubnubErrorSeverity.Critical, errorLevel);
            }
        }

        internal static void CommonExceptionHandler<T> (RequestState<T> requestState, string message, bool requestTimeout, 
            PubnubErrorFilter.Level errorLevel
        )
        {
            if (requestTimeout) {
                message = "Operation Timeout";
                #if (ENABLE_PUBNUB_LOGGING)
                LoggingMethod.WriteToLog (string.Format ("DateTime {0}, {1} response={2}", DateTime.Now.ToString (), 
                    requestState.RespType.ToString (), message), LoggingMethod.LevelInfo);
                #endif

                PubnubCallbacks.CallErrorCallback<T> (message, requestState, 
                    Helpers.GetTimeOutErrorCode (requestState.RespType), PubnubErrorSeverity.Critical, errorLevel);
            } else {
                #if (ENABLE_PUBNUB_LOGGING)
                LoggingMethod.WriteToLog (string.Format ("DateTime {0}, {1} response={2}", DateTime.Now.ToString (), requestState.RespType.ToString (), message), LoggingMethod.LevelInfo);
                #endif

                PubnubCallbacks.CallErrorCallback<T> (message, requestState, 
                    PubnubErrorCode.None, PubnubErrorSeverity.Critical, errorLevel);
            }
        }
    }
}
