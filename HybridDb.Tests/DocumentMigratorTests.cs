﻿using System;
using System.Collections.Generic;
using System.IO;
using HybridDb.Migration;
using Newtonsoft.Json;
using Newtonsoft.Json.Bson;
using Newtonsoft.Json.Linq;
using Xunit;
using Shouldly;

namespace HybridDb.Tests
{
    public class DocumentMigratorTests
    {
        [Fact]
        public void CanManipulateDocumentOnRead()
        {
            var serializer = new DefaultBsonSerializer();

            var migration = new Migration.Migration();
            migration.MigrateDocument()
                     .FromTable("Entities")
                     .ToVersion(2)
                     .RequireSchemaVersion(1)
                     .UseSerializer(serializer)
                     .MigrateOnRead<JObject>(x => x["Property"] = 10);

            var projections = new Dictionary<string, object>
            {
                {"Id", Guid.NewGuid()}, 
                {"Version", 1}, 
                {"Document", serializer.Serialize(JObject.Parse("{ Property: 1 }"))},
            };
            
            new DocumentMigrator().OnRead(migration, projections);

            var document = (JObject)serializer.Deserialize((byte[]) projections["Document"], typeof(JObject));
            ((int)document["Property"]).ShouldBe(10);
            ((int)projections["Version"]).ShouldBe(2);
        }

        [Fact]
        public void FailsOnReadIfCurrentDocumentIsWrongVersion()
        {
            var serializer = new DefaultBsonSerializer();

            var migration = new Migration.Migration();
            migration.MigrateDocument().FromTable("Entities").ToVersion(3);

            var projections = new Dictionary<string, object>
            {
                {"Id", Guid.NewGuid()}, 
                {"Version", 1}, 
                {"Document", serializer.Serialize(JObject.Parse("{ Property: 1 }"))},
            };
            
            Should.Throw<ArgumentException>(() => new DocumentMigrator().OnRead(migration, projections));
        }

        public class Entity
        {
            public Guid Id { get; set; }
            public int Property { get; set; }
        }
    }

    public static class JObjectEx
    {
        public static byte[] ToByteArray(this JObject self)
        {
            using (var sout = new MemoryStream())
            using (var bsonWriter = new BsonWriter(sout))
            {
                new JsonSerializer().Serialize(bsonWriter, self);
                return sout.ToArray();
            }
        }
    }
}