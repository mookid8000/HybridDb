﻿using System;

namespace HybridDb.Config
{
    public class SystemColumn : Column
    {
        public SystemColumn(string name, Type type, int? length = null, object defaultValue = null, bool isPrimaryKey = false) 
            : base(name, type, length, defaultValue, isPrimaryKey) { }
        
        public SystemColumn(string name, Type type) : base(name, type) { }
    }
}