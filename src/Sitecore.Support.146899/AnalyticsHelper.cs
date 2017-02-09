using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using MongoDB.Driver.Builders;
using Sitecore.Analytics;
using Sitecore.Analytics.Data.DataAccess.MongoDb;
using Sitecore.Analytics.Model;
using Sitecore.Diagnostics;

namespace Sitecore.Support
{
    public class AnalyticsHelper
    {
        public static void Identify(string identifier)
        {
            Assert.IsNotNull(Tracker.Current,"Tracker.Current != null");
            Assert.IsNotNull(Tracker.Current.Session, "Tracker.Current.Session != null");
            Tracker.Current.Session.Identify(identifier);
            MongoDbDriver driver = MongoDbDriver.FromConnectionString("analytics");
            var data = driver.Devices.FindOneAs<DeviceData>(Query.EQ("_id", Tracker.Current.Session.Device.DeviceId));
            if (data != null)
                driver.Devices.Update(Query.EQ("_id", Tracker.Current.Session.Device.DeviceId), Update.Set("LastKnownContactId", Tracker.Current.Contact.ContactId));
            else
            {
                driver.Devices.Insert(Tracker.Current.Session.Device);
            }
        }
    }
}