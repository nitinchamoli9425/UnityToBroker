using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using uPLibrary.Networking.M2Mqtt;
using uPLibrary.Networking.M2Mqtt.Messages;
using UnityMQTTROS2;

namespace UnityMQTTROS2.PanelChild
{
    public class Panel : UnityMQTT
    {
        // Start is called before the first frame update
        public GameObject PanelOpener;

        [Header("Panel configuration to connect to MQTT")]
        public Button startButton;
        public Button stop;
        public InputField brokerAddress;
        public InputField brokerPortNumber;
        public InputField topicsPublish;
        public InputField topicsSubscribe;
        public InputField qos;
        public InputField messagePublished;
        public InputField topicsUnsubscribe;
        public Button erase;
        public string[] publishTopic, msgPublish, subscribeTopic, unsubscribeTopic;
        // private MQTTClient unityMQTT;

        private List<string> eventMessages = new List<string>();

        //toogle button (MQTT) to open the panel
        public void openPanel()
        {
            if (PanelOpener != null)
            {
                bool isActive = PanelOpener.activeSelf;
                PanelOpener.SetActive(!isActive);
            }
        }

        public void InputBrokerAddress(string brokerAddressMQTT)
        {
            if (brokerAddress)
            {
                this.brokerAddressMQTT = brokerAddressMQTT;
                //Debug.Log("from input broker add" + brokerAddressMQTT);
            }
        }

        public void InputPortNumber(string portNumber)
        {
            if (brokerPortNumber)
            {
                int.TryParse(portNumber, out this.portNumber);
            }
        }

        public void InputPublishTopics(string publishTopics)
        {
            if (topicsPublish != null)
            {
                this.publishTopics = publishTopics;
                publishTopic = publishTopics.Split(',');
                Debug.LogFormat("publish topic is  " + this.publishTopics + "  publishtopic  " + publishTopic);
            }
        }

        public void InputMessagePublish(string messagePublish)
        {
            if (messagePublished != null)
            {
                this.messagePublish = messagePublish;
                msgPublish = messagePublish.Split(',');
            }
        }

        public void InputQOS(string qoss)
        { 
            if (qos != null)
                byte.TryParse(qoss, out this.qoss);
        }


        public void InputSubcribeTopics(string subscribeTopics)
        {
            if (topicsSubscribe != null)
            {
                this.subscribeTopics = subscribeTopics;
                subscribeTopic = subscribeTopics.Split(',');
               // Debug.Log("i am at subrcibe func");
            }
            //if (qos != null)
            //    byte.TryParse(qoss, out this.qoss);
        }

        public void InputUnsubscribeTopics(string unsubscribeTopics)
        {
            if (topicsUnsubscribe != null)
            {
                this.unsubscribeTopics = unsubscribeTopics;
                unsubscribeTopic = unsubscribeTopics.Split(',');
            }
        }

        protected override void WhenConnected()
        {
            Debug.LogFormat("Connected to the broker address \"" + brokerAddressMQTT + "\" at port \"" + portNumber + "\" \n");
            base.WhenConnected();
           /* PublishTopics();
            SubscribeTopics();
            UnsubscribeTopics(); */
        }

        protected override void PublishTopics()
        {
             Debug.Log("Publish topic function \n");
             for (int i = 0; i < publishTopic.Length; i++)
             {
                 Debug.Log("Publish topic function loop" + i + "\n");

                 unityMQTT.Publish(publishTopic[i], System.Text.Encoding.UTF8.GetBytes(msgPublish[i]), qoss, false);
                 Debug.Log("Test Message: \"" + msgPublish[i] + "\" is published");
             } 
            Debug.LogFormat("Publish topic function \n");
           // unityMQTT.Publish("nitin", System.Text.Encoding.UTF8.GetBytes("Hello from Unity!"), MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE, false);
        }

        protected override void SubscribeTopics()
        {
            Debug.LogFormat("Subscribe topic function \n");
            //do not forget to check qos later
            unityMQTT.Subscribe(subscribeTopic, new byte[] { MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE });
            for (int i = 0; i < subscribeTopic.Length; i++)
            {
                Debug.Log("Topics \"" + subscribeTopic[i] + "\" subscribed");
            }
        }

        protected override void UnsubscribeTopics()
        {

            unityMQTT.Unsubscribe(unsubscribeTopic);
            for (int i = 0; i < unsubscribeTopic.Length; i++)
            {
                Debug.Log("Topic(s) \"" + unsubscribeTopic[i] + "\" unsubscribed");
            }
        }

        private void UIInteract()
        {
            if (unityMQTT == null)
            {
                if (startButton != null)
                {
                    startButton.interactable = true;
                    stop.interactable = false;
                }
            }
            else
            {
                if (stop != null)
                {
                    stop.interactable = unityMQTT.IsConnected;
                }
                if (startButton != null)
                {
                    startButton.interactable = !unityMQTT.IsConnected;
                }
            }
            if (brokerAddress != null && startButton != null)
            {
                brokerAddress.interactable = startButton.interactable;
                brokerAddress.text = brokerAddressMQTT;
            }
            if (brokerPortNumber != null && startButton != null)
            {
                brokerPortNumber.interactable = startButton.interactable;
                brokerPortNumber.text = portNumber.ToString();
            }
            if (qos != null && startButton != null)
            {
                qos.interactable = startButton.interactable;
                qos.text = qoss.ToString();
            }
            if (topicsPublish != null && startButton != null)
            {
                topicsPublish.interactable = true;
                topicsPublish.text = publishTopics;
            }
            if (messagePublished != null && startButton != null)
            {
                messagePublished.interactable = startButton.interactable;
                messagePublished.text = messagePublish;
            }
            if (topicsSubscribe != null && startButton != null)
            {
                topicsSubscribe.interactable = startButton.interactable;
                topicsSubscribe.text = subscribeTopics;
            }
            if (topicsUnsubscribe != null && startButton != null)
            {
                topicsUnsubscribe.interactable = startButton.interactable;
                topicsUnsubscribe.text = unsubscribeTopics;
            }
            if (erase != null && startButton != null)
            {
                erase.interactable = startButton.interactable;
            }
        }

        protected override void DecodeMessage(string topic, byte[] message)
        {
            string msg = System.Text.Encoding.UTF8.GetString(message);
            Debug.Log("Received: " + msg);
            StoreMessage(msg);
        }

        private void StoreMessage(string eventMsg)
        {
            eventMessages.Add(eventMsg);
        }

        private void ProcessMessage(string msg)
        {
            Debug.Log("Received: " + msg);
        }

        protected override void Update()
        {
            base.Update(); // call ProcessMqttEvents()
            if (eventMessages.Count > 0)
            {
                foreach (string msg in eventMessages)
                {
                    ProcessMessage(msg);
                }
                eventMessages.Clear();
            }
        }

        private void OnDestroy()
        {
            Disconnect();
        }
        //private void onvalidate()
        //{
           
        //}
    }
}