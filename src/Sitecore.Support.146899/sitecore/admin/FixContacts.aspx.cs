using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Builders;
using Sitecore.Analytics.Data.DataAccess.MongoDb;
using Sitecore.Analytics.Tracking;
using Sitecore.Configuration;
using Sitecore.Analytics.Data;
using Sitecore.Analytics.Model;
using Sitecore.Analytics.Configuration;
using Sitecore.Data;
using Sitecore.Diagnostics;

namespace Sitecore.Support
{
  public partial class FixContacts : System.Web.UI.Page
  {
    MongoDbDriver driver = MongoDbDriver.FromConnectionString("analytics");
    public MongoCursor<BsonDocument> FindContacts()
    {
      IMongoQuery successorExists = Query.Exists("Successor");
      IMongoQuery systemExists = Query.Exists("System");
      IMongoQuery query = Query.And(successorExists, systemExists);
      return driver.Contacts.FindAs<BsonDocument>(query);
    }

    private bool CheckIfSuccessorContactCanBeLoaded(BsonValue successorContactId, List<Guid> contacts)
    {
        contacts.Add(successorContactId.AsGuid);

        var idQuery = Query.EQ("_id", successorContactId);
        var contact = driver.Contacts.FindOneAs<BsonDocument>(idQuery);

        if (contact == null)
        {
            Log.Warn($"Successor contact does not exist {successorContactId.AsGuid} and can't be loaded", this);
            return false;
        }

        if (contact.TryGetValue("Successor", out BsonValue successorValue))
        {
            //if (contact.TryGetElement("Identifiers", out BsonElement element) &&
            //    element.Value.AsBsonDocument.TryGetValue("Identifier", out _))
            //{
            //    Log.Warn($"Successor contact has both Successor and Identifiers.Identifier {successorContactId.AsGuid}",
            //        this);
            //    return false;
            //}

            if (contacts.Contains(successorValue.AsGuid))
            {
                StringBuilder sb = new StringBuilder();
                foreach (var id in contacts)
                {
                    sb.AppendLine(id.ToString());
                }

                Log.Warn($"Successor contacts loop is found: {sb}", this);
                return false;
            }

            return CheckIfSuccessorContactCanBeLoaded(successorValue, contacts);
        }

        return true;
    }

    protected void Button1_Click(object sender, EventArgs e)
    {
      var contacts = FindContacts();
      Log.Audit("Fixing contacts was started", this);
      ContactManager contactManager = Factory.CreateObject("tracking/contactManager", true) as ContactManager;
      ContactRepositoryBase contactRepository = Factory.CreateObject("tracking/contactRepository", true) as ContactRepositoryBase;
      int failedCounter = 0;
      int successfulCounter = 0;
      foreach (BsonDocument contact in contacts)
      {
        var successorId = contact["Successor"];
        var id = contact["_id"];

        Log.Audit($"Processing contact with id: {id.AsGuid} and Successor {successorId.AsGuid}", this);

        if (!CheckIfSuccessorContactCanBeLoaded(successorId, new List<Guid>()))
        {
            Log.Warn($"Processing contact with id: {id.AsGuid} and Successor {successorId.AsGuid} failed. Skipping it.", this);
            failedCounter++;
            continue;
        }

        var survivingContact = contactManager.LoadContactReadOnly(successorId.AsGuid);


        if (survivingContact == null)
        {
          Log.Audit("Surviving contact is null " + successorId.AsGuid, this);
          continue;
        }

        //remove incorrect Successor and Identifiers fields.
        driver.Contacts.Update(Query.EQ("_id", id), Update.Unset("Successor").Unset("Identifiers"));

        var dyingContact = contactManager.LoadContactReadOnly(id.AsGuid);
        //Merging contacts
        contactManager.MergeContacts(survivingContact, dyingContact);
        contactManager.ReleaseContact(dyingContact.ContactId);
        contactManager.GetType().GetMethod("RemoveFromSession", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).Invoke(contactManager, new object[] { dyingContact.ContactId });

        //Obsoleting dying contact
        LeaseOwner leaseOwner = new LeaseOwner(AnalyticsSettings.ClusterName, LeaseOwnerType.WebCluster);
        contactRepository.ObsoleteContact(dyingContact, leaseOwner, new ID(survivingContact.ContactId));
        successfulCounter++;
      }

      Log.Audit("Fixing contacts was finished", this);
      
      Label1.Text = $"Completed. Failed: {failedCounter}. Successful: {successfulCounter}";
    }

    protected void Button2_Click(object sender, EventArgs e)
    {
      var contacts = FindContacts();
      Label1.Text = "Found " + contacts.Count().ToString() + " corrupted contacts";
    }
  }
}