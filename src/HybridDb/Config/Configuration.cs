﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using HybridDb.Migrations;
using HybridDb.Serialization;
using Serilog;
using static Indentional.Indent;

namespace HybridDb.Config
{
    public class Configuration
    {
        readonly object gate = new object();

        bool initialized;

        internal Configuration()
        {
            Tables = new ConcurrentDictionary<string, Table>();
            DocumentDesigns = new List<DocumentDesign>();

            Logger = Log.Logger;

            Serializer = new DefaultSerializer();
            TypeMapper = new AssemblyQualifiedNameTypeMapper();
            Migrations = new List<Migration>();
            BackupWriter = new NullBackupWriter();
            RunSchemaMigrationsOnStartup = true;
            RunDocumentMigrationsOnStartup = true;
            TableNamePrefix = "";
            DefaultKeyResolver = KeyResolver;
        }

        public ILogger Logger { get; private set; }
        public ISerializer Serializer { get; private set; }
        public ITypeMapper TypeMapper { get; private set; }
        public IReadOnlyList<Migration> Migrations { get; private set; }
        public IBackupWriter BackupWriter { get; private set; }
        public bool RunSchemaMigrationsOnStartup { get; private set; }
        public bool RunDocumentMigrationsOnStartup { get; private set; }
        public int ConfiguredVersion { get; private set; }
        public string TableNamePrefix { get; private set; }
        public Func<object, string> DefaultKeyResolver { get; private set; }

        internal ConcurrentDictionary<string, Table> Tables { get; }
        internal List<DocumentDesign> DocumentDesigns { get; }

        static string GetTableNameByConventionFor(Type type)
        {
            return Inflector.Inflector.Pluralize(type.Name);
        }

        internal void Initialize()
        {
            lock (gate)
            {
                DocumentDesigns.Insert(0, new DocumentDesign(this, GetOrAddTable("Documents"), typeof(object), "object"));

                initialized = true;
            }
        }

        public DocumentDesigner<TEntity> Document<TEntity>(string tablename = null)
        {
            return new DocumentDesigner<TEntity>(GetOrCreateDesignFor(typeof (TEntity), tablename));
        }

        public DocumentDesign GetOrCreateDesignFor(Type type, string tablename = null)
        {
            lock (gate)
            {
                // for interfaces we find the first design for a class that is assignable to the interface or fallback to the design for typeof(object)
                if (type.IsInterface)
                {
                    return DocumentDesigns.FirstOrDefault(x => type.IsAssignableFrom(x.DocumentType)) ?? DocumentDesigns[0];
                }

                //TODO: Table equals base design... model it?
                var existing = TryGetDesignFor(type);

                // no design for type, nor a base design, add new table and base design
                if (existing == null)
                {
                    return AddDesign(new DocumentDesign(
                        this, GetOrAddTable(tablename ?? GetTableNameByConventionFor(type)), 
                        type, TypeMapper.ToDiscriminator(type)));
                }

                // design already exists for type
                if (existing.DocumentType == type)
                {
                    if (tablename == null || tablename == existing.Table.Name)
                        return existing;

                    throw new InvalidOperationException(_($@"
                        Design already exists for type '{type}' but is not assigned to the specified tablename '{tablename}'.
                        The existing design for '{type}' is assigned to table '{existing.Table.Name}'."));
                }

                // we now know that type is a subtype to existing
                // there is explicitly given a table name, so we add a new table for the derived type
                if (tablename != null)
                {
                    return AddDesign(new DocumentDesign(
                        this, GetOrAddTable(tablename),
                        type, TypeMapper.ToDiscriminator(type)));
                }

                // a table and base design exists for type, add the derived type as a child design
                var design = new DocumentDesign(this, existing, type, TypeMapper.ToDiscriminator(type));

                var afterParent = DocumentDesigns.IndexOf(existing) + 1;
                DocumentDesigns.Insert(afterParent, design);

                return design;
            }
        }

        internal DocumentDesign GetOrCreateDesignByDiscriminator(DocumentDesign design, string discriminator)
        {
            lock (gate)
            {
                DocumentDesign concreteDesign;
                if (design.DecendentsAndSelf.TryGetValue(discriminator, out concreteDesign))
                    return concreteDesign;

                var type = TypeMapper.ToType(discriminator);

                if (type == null)
                {
                    throw new InvalidOperationException($"No concrete type could be mapped from discriminator '{discriminator}'.");
                }

                return GetOrCreateDesignFor(type);
            }
        }

        public DocumentDesign GetDesignFor<T>()
        {
            var design = TryGetDesignFor(typeof(T));
            if (design != null) return design;

            throw new HybridDbException(string.Format(
                "No design was registered for documents of type {0}. " +
                "Please run store.Document<{0}>() to register it before use.", 
                typeof(T).Name));
        }

        public DocumentDesign TryGetDesignFor(Type type)
        {
            // get most specific type by searching backwards
            lock (gate)
            {
                return DocumentDesigns.LastOrDefault(x => x.DocumentType.IsAssignableFrom(type));
            }
        }

        public DocumentDesign GetExactDesignFor(Type type)
        {
            lock (gate)
            {
                return DocumentDesigns.First(x => x.DocumentType == type);
            }
        }

        public DocumentDesign TryGetDesignByTablename(string tablename)
        {
            lock (gate)
            {
                return DocumentDesigns.FirstOrDefault(x => x.Table.Name == tablename);
            }
        }

        public void UseLogger(ILogger logger)
        {
            Logger = logger;
        }

        public void UseSerializer(ISerializer serializer)
        {
            Serializer = serializer;
        }

        public void UseTypeMapper(ITypeMapper typeMapper)
        {
            lock (gate)
            {
                if (DocumentDesigns.Any())
                    throw new InvalidOperationException("Please call UseTypeMapper() before any documents are configured.");

                TypeMapper = typeMapper;
            }
        }

        public void UseMigrations(IReadOnlyList<Migration> migrations)
        {
            Migrations = migrations.OrderBy(x => x.Version).Where((x, i) =>
            {
                var expectedVersion = i+1;
                
                if (x.Version == expectedVersion)
                    return true;
                
                throw new ArgumentException($"Missing migration for version {expectedVersion}.");
            }).ToList();

            ConfiguredVersion = Migrations.Any() ? Migrations.Last().Version : 0;
        }

        public void UseBackupWriter(IBackupWriter backupWriter)
        {
            BackupWriter = backupWriter;
        }

        public void UseTableNamePrefix(string prefix)
        {
            TableNamePrefix = prefix;
        }

        public void UseKeyResolver(Func<object, string> resolver)
        {
            DefaultKeyResolver = resolver;
        }

        internal void DisableMigrationsOnStartup()
        {
            RunSchemaMigrationsOnStartup = false;
            RunDocumentMigrationsOnStartup = false;
        }

        public void DisableDocumentMigrationsOnStartup()
        {
            RunDocumentMigrationsOnStartup = false;
        }

        static string KeyResolver(object entity)
        {
            var id = ((dynamic)entity).Id;
            return id != null ? id.ToString() : Guid.NewGuid().ToString();
        }

        DocumentDesign AddDesign(DocumentDesign design)
        {
            var existingDesign = DocumentDesigns.FirstOrDefault(x => design.DocumentType.IsAssignableFrom(x.DocumentType));
            if (existingDesign != null)
            {
                throw new InvalidOperationException($"Document {design.DocumentType.Name} must be configured before its subtype {existingDesign.DocumentType}.");
            }

            DocumentDesigns.Add(design);
            return design;
        }

        DocumentTable GetOrAddTable(string tablename)
        {
            if (tablename == null) throw new ArgumentNullException(nameof(tablename));

            return (DocumentTable)Tables.GetOrAdd(tablename, name =>
            {
                if (initialized)
                {
                    throw new InvalidOperationException($"You can not register the table '{tablename}' after store has been initialized.");
                }

                return new DocumentTable(name);
            });
        }
    }
}