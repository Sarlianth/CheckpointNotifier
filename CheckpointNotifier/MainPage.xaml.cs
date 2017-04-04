using System;
using System.Collections.Generic;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Maps;
using Windows.Devices.Geolocation;
using Windows.Services.Maps;
using winsdkfb;
using winsdkfb.Graph;
using Newtonsoft.Json;
using Windows.UI.Xaml.Media;
using Windows.UI;

namespace CheckpointNotifier
{
    public sealed partial class MainPage : Page
    {
        public Geoposition geoposition = null;
        public String url;
        public String checkpointAt;
        Geolocator geolocator = null;
        Windows.Storage.ApplicationDataContainer localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
        Windows.Storage.StorageFolder localFolder = Windows.Storage.ApplicationData.Current.LocalFolder;    

        public MainPage()
        {
            this.InitializeComponent();

            geolocator = new Geolocator();
            geolocator.DesiredAccuracy = PositionAccuracy.High;
            geolocator.MovementThreshold = 20; // The units are meters.

            geolocator.StatusChanged += geolocator_StatusChanged;
            geolocator.PositionChanged += geolocator_PositionChanged;

            Login();
            if(localSettings.Values["counter"] == null)
            {
                localSettings.Values["counter"] = 0;
            }
            else
            {
                myTextBlock.Text += "You have already reported" + localSettings.Values["counter"] + " times";
            }
        }

        async void Login()
        {
            FBSession sess = FBSession.ActiveSession;
            sess.FBAppId = "834073723397876";
            sess.WinAppId = "1aa0d5698f864a5fb29fcef83c8ab990";
            
            // Add permissions required by the app
            List<String> permissionList = new List<String>();
            permissionList.Add("public_profile");
            permissionList.Add("user_friends");
            permissionList.Add("publish_actions");
            FBPermissions permissions = new FBPermissions(permissionList);

            // Login to Facebook
            FBResult result = await sess.LoginAsync(permissions);

            FBUser user = sess.User;

            if (result.Succeeded)
            {
                //Login successful
                myTextBlock.Foreground = new SolidColorBrush(Colors.Chartreuse);
                myTextBlock.Text = "Hi, " + user.Name + "\n" + "You have already reported " + localSettings.Values["counter"] + " times";
            }
            else
            {
                //Login failed
                myTextBlock.Foreground = new SolidColorBrush(Colors.Crimson);
                myTextBlock.Text = "Please login in order to use this app";
                Login();
            }
        }

        async void Logout()
        {
            FBSession sess = FBSession.ActiveSession;
            await sess.LogoutAsync();
            myTextBlock.Foreground = new SolidColorBrush(Colors.Chartreuse);
            myTextBlock.Text = "You have successfully logged out";
        }

        private void reportCheckpoint_Click(object sender, RoutedEventArgs e)
        {
            FBSession sess = FBSession.ActiveSession;
            // If the user is logged in
            if (sess.LoggedIn)
            {
                url = "http://dev.virtualearth.net/REST/v1/Imagery/Map/Road/"
                    + geoposition.Coordinate.Point.Position.Latitude + ","
                    + geoposition.Coordinate.Point.Position.Longitude
                    + "/14?mapSize=500,500"
                    + "&pushpin="
                    + geoposition.Coordinate.Point.Position.Latitude + ","
                    + geoposition.Coordinate.Point.Position.Longitude
                    + ";35"
                    + "&key=2AURQARVHdCh6XKUyT43~_ozQdexfSj_HkfFzo_pvtw~An0kXb14UEY0FZDpIp8BbstoVoRF7oErN86HkkYkE1Yg--G37k7A27Pj0AixZ9c4";

                reportCheckpoint();
                localSettings.Values["counter"] = (int)localSettings.Values["counter"] + 1;
            }
            else
            {
                myTextBlock.Foreground = new SolidColorBrush(Colors.Crimson);
                myTextBlock.Text = "Reporting failed, make sure you login first with Facebook\nPlease try again";
                Login();
            }                
        }

        private void reportSpeedvan_Click(object sender, RoutedEventArgs e)
        {
            FBSession sess = FBSession.ActiveSession;
            // If the user is logged in
            if (sess.LoggedIn)
            {
                url = "http://dev.virtualearth.net/REST/v1/Imagery/Map/Road/"
                    + geoposition.Coordinate.Point.Position.Latitude + ","
                    + geoposition.Coordinate.Point.Position.Longitude
                    + "/14?mapSize=500,500"
                    + "&pushpin="
                    + geoposition.Coordinate.Point.Position.Latitude + ","
                    + geoposition.Coordinate.Point.Position.Longitude
                    + ";61"
                    + "&key=2AURQARVHdCh6XKUyT43~_ozQdexfSj_HkfFzo_pvtw~An0kXb14UEY0FZDpIp8BbstoVoRF7oErN86HkkYkE1Yg--G37k7A27Pj0AixZ9c4";

                reportSpeedvan();
                localSettings.Values["counter"] = (int)localSettings.Values["counter"] + 1;
            }
            else
            {
                myTextBlock.Foreground = new SolidColorBrush(Colors.Crimson);
                myTextBlock.Text = "Reporting failed, make sure you login first with Facebook\nPlease try again";
                Login();
            }    
        }

        private void facebookLogin_Click(object sender, RoutedEventArgs e)
        {
            Login();
        }

        private void facebookLogout_Click(object sender, RoutedEventArgs e)
        {
            Logout();
        }

        public class FBReturnObject
        {
            public string Id { get; set; }
            public string Post_Id { get; set; }
        }

        async void reportCheckpoint()
        {
            var position = await geolocator.GetGeopositionAsync();
            var mapLocation = await MapLocationFinder.FindLocationsAtAsync(position.Coordinate.Point);
            if (mapLocation.Status == MapLocationFinderStatus.Success)
            {
                checkpointAt = mapLocation?.Locations?[0].Address.FormattedAddress;
            }
            else
            {

            }
            // Get active session
            FBSession sess = FBSession.ActiveSession;
            // If the user is logged in
            if (sess.LoggedIn)
            {
                // Get current user
                FBUser user = sess.User;
                // Set caption, link and description parameters
                PropertySet parameters = new PropertySet();
                parameters.Add("title", "Checkpoint Notifier");
                parameters.Add("description", "I have just passed a checkpoint");
                parameters.Add("link", url);
                // Add post message
                parameters.Add("message", "Checkpoint at: " + checkpointAt);
                // Set Graph api path
                string path = "/" + user.Id + "/feed";
                var factory = new FBJsonClassFactory(s =>
                {
                    return JsonConvert.DeserializeObject<FBReturnObject>(s);
                });
                var singleValue = new FBSingleValue(path, parameters, factory);
                var result = await singleValue.PostAsync();
                if (result.Succeeded)
                {
                    var response = result.Object as FBReturnObject;
                    myTextBlock.Foreground = new SolidColorBrush(Colors.Chartreuse);
                    myTextBlock.Text = user.Name+", you have successfully reported a checkpoint\nThank you";
                }
                else
                {
                    // Posting failed
                    myTextBlock.Foreground = new SolidColorBrush(Colors.Crimson);
                    myTextBlock.Text = "Reporting failed, make sure you login first with Facebook\nPlease try again";
                    Login();
                }
            }
        }

        async void reportSpeedvan()
        {
            var position = await geolocator.GetGeopositionAsync();
            var mapLocation = await MapLocationFinder.FindLocationsAtAsync(position.Coordinate.Point);
            if (mapLocation.Status == MapLocationFinderStatus.Success)
            {
                checkpointAt = mapLocation?.Locations?[0].Address.FormattedAddress;
            }
            else
            {

            }
                // Get active session
                FBSession sess = FBSession.ActiveSession;
            // If the user is logged in
            if (sess.LoggedIn)
            {
                // Get current user
                FBUser user = sess.User;
                // Set caption, link and description parameters
                PropertySet parameters = new PropertySet();
                parameters.Add("title", "Checkpoint Notifier");
                parameters.Add("description", "I have just passed a speed camera");
                parameters.Add("link", url);
                // Add post message
                parameters.Add("message", "Speed camera at: " + checkpointAt);
                // Set Graph api path
                string path = "/" + user.Id + "/feed";
                var factory = new FBJsonClassFactory(s =>
                {
                    return JsonConvert.DeserializeObject<FBReturnObject>(s);
                });
                var singleValue = new FBSingleValue(path, parameters, factory);
                var result = await singleValue.PostAsync();
                if (result.Succeeded)
                {
                    var response = result.Object as FBReturnObject;
                    myTextBlock.Foreground = new SolidColorBrush(Colors.Chartreuse);
                    myTextBlock.Text = user.Name + ", you have successfully reported a speed camera\nThank you";
                }
                else
                {
                    // Posting failed
                    myTextBlock.Foreground = new SolidColorBrush(Colors.Crimson);
                    myTextBlock.Text = "Reporting failed, make sure you login first with Facebook\nPlease try again";
                    Login();
                }
            }
        }

        private async void refresh(){
            var position = await geolocator.GetGeopositionAsync();

            var mapLocation = await MapLocationFinder.FindLocationsAtAsync(position.Coordinate.Point);
            if (mapLocation.Status == MapLocationFinderStatus.Success)
            {
                Map.MapElements.Clear();
                geoposition = await geolocator.GetGeopositionAsync();
                Map.Center = geoposition.Coordinate.Point;
                MapIcon myPOI = new MapIcon {
                    Location = geoposition.Coordinate.Point,
                    Title = "You"};

                Map.MapElements.Add(myPOI);
            }
            else
            {
                
            }
        }

        /*
        private void map_Tapped(Windows.UI.Xaml.Controls.Maps.MapControl sender, Windows.UI.Xaml.Controls.Maps.MapInputEventArgs args)
        {
            var tappedGeoPosition = args.Location.Position;
            string status = "MapTapped at \nLatitude:" + tappedGeoPosition.Latitude + "\nLongitude: " + tappedGeoPosition.Longitude;

            // Specify a known location.
            BasicGeoposition snPosition = new BasicGeoposition() { Latitude = tappedGeoPosition.Latitude, Longitude = tappedGeoPosition.Longitude };
            Geopoint snPoint = new Geopoint(snPosition);

            // Create a MapIcon.
            MapIcon mapIcon1 = new MapIcon();
            mapIcon1.Location = snPoint;
            mapIcon1.NormalizedAnchorPoint = new Point(0.5, 1.0);
            mapIcon1.Title = "Check Point";
            mapIcon1.ZIndex = 0;

            // Add the MapIcon to the map.
            Map.MapElements.Add(mapIcon1);
        }
        */

        private void MyMap_Loaded(object sender, RoutedEventArgs e)
        {
            Map.ZoomLevel = 13;
        }

        async void geolocator_StatusChanged(Geolocator sender, StatusChangedEventArgs args)
        {
            string status = "";

            switch (args.Status)
            {
                case PositionStatus.Disabled:
                    // the application does not have the right capability or the location master switch is off
                    status = "location is disabled in phone settings";
                    break;
                case PositionStatus.Initializing:
                    // the geolocator started the tracking operation
                    status = "initializing";
                    break;
                case PositionStatus.NoData:
                    // the location service was not able to acquire the location
                    status = "no data";
                    break;
                case PositionStatus.Ready:
                    // the location service is generating geopositions as specified by the tracking parameters
                    status = "ready";
                    break;
                case PositionStatus.NotAvailable:
                    status = "not available";
                    // not used in WindowsPhone, Windows desktop uses this value to signal that there is no hardware capable to acquire location information
                    break;
                case PositionStatus.NotInitialized:
                    // the initial state of the geolocator, once the tracking operation is stopped by the user the geolocator moves back to this state

                    break;
            }

            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                myTextBlock.Text += status;
            });
        }

        async void geolocator_PositionChanged(Geolocator sender, PositionChangedEventArgs args)
        {
            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                refresh();
            });
        }
    }
}