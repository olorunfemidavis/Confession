﻿using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Text;

namespace Shared
{
    public class DeviceInfo
    {
        [BsonId]
        [BsonRepresentation(MongoDB.Bson.BsonType.ObjectId)]
        public string Id { get; set; } = ObjectId.GenerateNewId().ToString();
        public string Model { get; set; } = string.Empty;
        public string Manufacturer { get; set; } = string.Empty;
        public string VersionString { get; set; } = string.Empty;
        public string Platform { get; set; } = string.Empty;
        public string Idiom { get; set; } = string.Empty;
        public string DeviceType { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;

    }
}
