﻿using Xamarin.Forms;

namespace Mobile.Helpers
{
    public class AppConstants
    {
        public static string AppId
        {
            get
            {
                switch (Device.RuntimePlatform)
                {
                    case Device.Android:
                        return "ca-app-pub-4507736790505069~9315189412";
                    default:
                        return "ca-app-pub-4507736790505069~9315189412";
                }
            }
        }

        /// <summary>
        /// These Ids are test Ids from https://developers.google.com/admob/android/test-ads
        /// </summary>
        /// <value>The banner identifier.</value>
        public static string HomeBannerId
        {

            get
            {
                switch (Device.RuntimePlatform)
                {
                    case Device.Android:
                        return "ca-app-pub-4507736790505069/4350778903"; //old id return "ca -app-pub-4507736790505069/3601851826";
                    default:
                        return "ca-app-pub-4507736790505069/4350778903"; //old idreturn "ca-app-pub-4507736790505069/3601851826";
                }
                //switch (Device.RuntimePlatform)
                //{
                //    case Device.Android:
                //        return "ca-app-pub-3940256099942544/6300978111";
                //    default:
                //        return "ca-app-pub-3940256099942544/6300978111";
                //}
            }
        }

        public static string CommentBannerId
        {
            get
            {
                switch (Device.RuntimePlatform)
                {
                    case Device.Android:
                        return "ca-app-pub-4507736790505069/8783931635";
                    default:
                        return "ca-app-pub-4507736790505069/8783931635";
                }

            }
        }
        public static string ViewPageBannerId
        {
            get
            {
                switch (Device.RuntimePlatform)
                {
                    case Device.Android:
                        return "ca-app-pub-4507736790505069/4405702921";
                    default:
                        return "ca-app-pub-4507736790505069/4405702921";
                }

            }
        }

        /// <summary>
        /// These Ids are test Ids from https://developers.google.com/admob/android/test-ads
        /// </summary>
        /// <value>The Interstitial Ad identifier.</value>
        public static string InterstitialAdId
        {
            get
            {
                switch (Device.RuntimePlatform)
                {
                    case Device.Android:
                        return "ca-app-pub-4507736790505069/6283518118";// "ca-app-pub-4507736790505069/8628793705";
                    default:
                        return "ca-app-pub-4507736790505069/6283518118";// "ca-app-pub-4507736790505069/8628793705";
                }
                //switch (Device.RuntimePlatform)
                //{
                //    case Device.Android:
                //        return "ca-app-pub-3940256099942544/1033173712";
                //    default:
                //        return "ca-app-pub-3940256099942544/1033173712";
                //}
            }
        }

        private static int _adCounter;
        public static bool ShowAds
        {
            get
            {
                _adCounter++;
                if (_adCounter % 10 == 0)
                {
                    return true;
                }
                return false;
            }
        }


        private static int _postCounter;
        public static bool ShowPostConfession
        {
            get
            {
                _postCounter++;
                if (_postCounter % 5 == 0)
                {
                    return true;
                }
                return false;
            }
        }

        private static int _deleteCounter;
        public static bool ShowDeleteConfession
        {
            get
            {
                _deleteCounter++;
                if (_deleteCounter % 15 == 0)
                {
                    return true;
                }
                return false;
            }
        }

        private static int _makeRegistration;
        public static bool MakeRegistration
        {
            get
            {
                _makeRegistration++;
                if (_makeRegistration == 1)
                {
                    return true;
                }
                return false;
            }
        }

        private static string _activityID;

        public static double SwipeValue { get; set; }

        public static bool GetSwipe(string id, double value)
        {
            if (id != _activityID)
            {
                if (value >= 300)
                {
                    SwipeValue = 0;
                    _activityID = id;
                    return true;
                }
                else
                {
                    SwipeValue = value;
                    _activityID = id;
                    return false;
                }

            }
            else
            {
                if (SwipeValue >= 300)
                {
                    SwipeValue = 0;
                    return true;
                }
                else
                {
                    SwipeValue = SwipeValue + value;
                    return false;
                }
            }
        }
    }
}
