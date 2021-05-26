using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using uPLibrary.Networking.M2Mqtt;
using uPLibrary.Networking.M2Mqtt.Messages;

namespace UnityMQTTROS2
{
    public class UnityMQTT : MonoBehaviour
    {
        //define variables whose values are obtained from UI
        public string brokerAddressMQTT;
        public int portNumber;
        public bool isEncrypted = false;
        public int connectionDelay = 500;
        public string publishTopics, subscribeTopics, unsubscribeTopics, messagePublish;
        public byte qoss;
        public int timeout = MqttSettings.MQTT_CONNECT_TIMEOUT;
        //Note: later add username passwword

        //create instance
        protected MqttClient unityMQTT;

        private List<MqttMsgPublishEventArgs> messageQueue1 = new List<MqttMsgPublishEventArgs>(); 
        private List<MqttMsgPublishEventArgs> messageQueue2 = new List<MqttMsgPublishEventArgs>();
        private List<MqttMsgPublishEventArgs> frontMessageQueue = null;
        private List<MqttMsgPublishEventArgs> backMessageQueue = null;
        private bool mqttClientConnectionClosed = false;
        private bool mqttClientConnected = false;

        //Event action when connection is successful
        public event Action ConnectionSucceeded;

        //Event action when connection is unsuccessful
        public event Action ConnectionFailed;

        public virtual void Connect()
        {
            if (unityMQTT == null || !unityMQTT.IsConnected)
            {
               // Debug.Log("this works!! from input broker add" + this.brokerAddressMQTT  + " this:--" + brokerAddressMQTT);
                StartCoroutine(DoConnect());
            }
        }

        public virtual void Disconnect()
        {
            if (unityMQTT != null)
            {
                StartCoroutine(DoDisconnect());
            }
        }

        protected virtual void WhenConnected()
        {
            PublishTopics();
            SubscribeTopics();

            if (ConnectionSucceeded != null)
            {
                ConnectionSucceeded();
            }
        }

        protected virtual void WhenConnectionFails(string errorMessage)
        {
            Debug.LogWarning("Connection failed." + errorMessage);
            if (ConnectionFailed != null)
            {
                ConnectionFailed();
            }
        }

        protected virtual void PublishTopics()
        {
            //Debug.Log("hi from Publish base class function");
        }

        protected virtual void SubscribeTopics()
        {
            //Debug.Log("hi from subcribe base class function");
        }

        protected virtual void UnsubscribeTopics()
        {

        }

        protected virtual void OnApplicationQuit()
        {
            CloseConnection();
        }

        protected virtual void Awake()
        {
            frontMessageQueue = messageQueue1;
            backMessageQueue = messageQueue2;
        }
        //        client.publish();
        protected virtual void start()
        {
        }
        protected virtual void DecodeMessage(string topic, byte[] message)
        {
        }

        protected virtual void OnDisconnected()
        {
            Debug.Log("Disconnected.");
        }

        protected virtual void OnConnectionLost()
        {
            Debug.LogWarning("CONNECTION LOST!");
        }

        protected virtual void Update()
        {
            ProcessMqttEvents();
        }

        protected virtual void ProcessMqttEvents()
        {
            // process messages in the main queue
            SwapMqttMessageQueues();
            ProcessMqttMessageBackgroundQueue();
            // process messages income in the meanwhile
            SwapMqttMessageQueues();
            ProcessMqttMessageBackgroundQueue();

            if (mqttClientConnectionClosed)
            {
                mqttClientConnectionClosed = false;
                OnConnectionLost();
            }
        }

        private void ProcessMqttMessageBackgroundQueue()
        {
            foreach (MqttMsgPublishEventArgs msg in backMessageQueue)
            {
                DecodeMessage(msg.Topic, msg.Message);
            }
            backMessageQueue.Clear();
        }

        private void SwapMqttMessageQueues()
        {
            frontMessageQueue = frontMessageQueue == messageQueue1 ? messageQueue2 : messageQueue1;
            backMessageQueue = backMessageQueue == messageQueue1 ? messageQueue2 : messageQueue1;
        }

        private void OnMqttMessageReceived(object sender, MqttMsgPublishEventArgs msg)
        {
            frontMessageQueue.Add(msg);
        }

        private void OnMqttConnectionClosed(object sender, EventArgs e)
        {
            // Set unexpected connection closed only if connected (avoid event handling in case of controlled disconnection)
            mqttClientConnectionClosed = mqttClientConnected;
            mqttClientConnected = false;
        }

        private IEnumerator DoConnect()
        {
            // wait for the given delay
            yield return new WaitForSecondsRealtime(connectionDelay / 1000f);
            // leave some time to Unity to refresh the UI
            yield return new WaitForEndOfFrame();

            // create client instance 
            if (unityMQTT == null)
            {
                try
                {
                   // Debug.LogFormat("brioker address " + brokerAddressMQTT + " port number" + portNumber + " topicd publish " + publishTopics + " msg publish " + messagePublish);
                    unityMQTT = new MqttClient(brokerAddressMQTT, portNumber, false, null, null, false ? MqttSslProtocols.SSLv3 : MqttSslProtocols.None);
                }
                catch (Exception e)
                {
                    unityMQTT = null;
                    Debug.LogErrorFormat("CONNECTION FAILED! {0}", e.ToString());
                    WhenConnectionFails(e.Message);
                    yield break;
                }
            }
            else if (unityMQTT.IsConnected)
            {
                yield break;
            }

            // leave some time to Unity to refresh the UI
            yield return new WaitForEndOfFrame();  
            yield return new WaitForEndOfFrame();

            unityMQTT.Settings.TimeoutOnConnection = timeout;
            string clientId = Guid.NewGuid().ToString();
            try
            {
                unityMQTT.Connect(clientId, null, null);
            }
            catch (Exception e)
            {
                unityMQTT = null;
                Debug.LogErrorFormat("Failed to connect to {0}:{1}:\n{2}", brokerAddressMQTT, portNumber, e.ToString());
                WhenConnectionFails(e.Message);
                yield break;
            }
            if (unityMQTT.IsConnected)
            {
                unityMQTT.ConnectionClosed += OnMqttConnectionClosed;
                // register to message received 
                unityMQTT.MqttMsgPublishReceived += OnMqttMessageReceived;
                mqttClientConnected = true;
                WhenConnected();
            }
            else
            {
                WhenConnectionFails("CONNECTION FAILED!");
            }
        }

        private IEnumerator DoDisconnect()
        {
            yield return new WaitForEndOfFrame();
            CloseConnection();
            OnDisconnected();
        }

        private void CloseConnection()
        {
            mqttClientConnected = false;
            if (unityMQTT != null)
            {
                if (unityMQTT.IsConnected)
                {
                    UnsubscribeTopics();
                    unityMQTT.Disconnect();
                }
                unityMQTT.MqttMsgPublishReceived -= OnMqttMessageReceived;
                unityMQTT.ConnectionClosed -= OnMqttConnectionClosed;
                unityMQTT = null;
            }
        }
    }
}