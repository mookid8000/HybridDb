﻿using System.Data;
using HybridDb.Config;
using HybridDb.Migration.Commands;
using Shouldly;
using Xunit.Extensions;

namespace HybridDb.Tests
{
    public class SchemaTests : HybridDbTests
    {
        [Theory]
        [InlineData(TableMode.UseTempTables)]
        [InlineData(TableMode.UseRealTables)]
        public void ReturnsAllTables(TableMode mode)
        {
            Use(mode);

            new CreateTable(new Table("Entities1", new Column("test", typeof(int)))).Execute(store);
            new CreateTable(new Table("Entities2", new Column("test", typeof(int)))).Execute(store);

            var schema = store.Schema.GetSchema();

            schema["Entities1"].ShouldNotBe(null);
            schema["Entities2"].ShouldNotBe(null);
        }

        [Theory]
        [InlineData(TableMode.UseTempTables)]
        [InlineData(TableMode.UseRealTables)]
        public void ReturnsAllColumns(TableMode mode)
        {
            Use(mode);

            new CreateTable(new Table("Entities1", new Column("test", typeof(int)))).Execute(store);
            new AddColumn("Entities1", new Column("SomeInt", typeof(int))).Execute(store);
            new AddColumn("Entities1", new Column("SomeBool", typeof(bool))).Execute(store);

            new CreateTable(new Table("Entities2", new Column("test", typeof(int)))).Execute(store);
            new AddColumn("Entities2", new Column("SomeString", typeof(string))).Execute(store);

            var schema = store.Schema.GetSchema();

            schema["Entities1"]["SomeInt"].ShouldNotBe(null);
            schema["Entities1"]["SomeBool"].ShouldNotBe(null);
            schema["Entities2"]["SomeString"].ShouldNotBe(null);
        }

        [Theory]
        [InlineData(TableMode.UseTempTables)]
        [InlineData(TableMode.UseRealTables)]
        public void ColumnsHasTypeInfo(TableMode mode)
        {
            Use(mode);

            new CreateTable(new Table("Entities1", new Column("test", typeof(int)))).Execute(store);
            new AddColumn("Entities1", new Column("SomeInt", typeof(int))).Execute(store);
            new AddColumn("Entities1", new Column("SomeBool", typeof(bool))).Execute(store);
            new AddColumn("Entities1", new Column("SomeString", typeof(string))).Execute(store);

            var schema = store.Schema.GetSchema();

            schema["Entities1"]["SomeInt"].Type.ShouldBe(typeof(int));
            schema["Entities1"]["SomeBool"].Type.ShouldBe(typeof(bool));
            schema["Entities1"]["SomeString"].Type.ShouldBe(typeof(string));
        }

        [Theory]
        [InlineData(TableMode.UseTempTables)]
        [InlineData(TableMode.UseRealTables)]
        public void ColumnsHasNullableInfo(TableMode mode)
        {
            Use(mode);

            new CreateTable(new Table("Entities1", new Column("test", typeof(int)))).Execute(store);
            new AddColumn("Entities1", new Column("SomeNullableInt", typeof(int), new SqlColumn(DbType.Int32, nullable: true))).Execute(store);
            new AddColumn("Entities1", new Column("SomeString", typeof(string), new SqlColumn(DbType.String, nullable: true))).Execute(store);
            new AddColumn("Entities1", new Column("SomeInt", typeof(int), new SqlColumn(DbType.Int32, nullable: false))).Execute(store);

            var schema = store.Schema.GetSchema();

            schema["Entities1"]["SomeNullableInt"].Nullable.ShouldBe(true);
            schema["Entities1"]["SomeInt"].Nullable.ShouldBe(false);
            schema["Entities1"]["SomeString"].Nullable.ShouldBe(true);
        }

        [Theory]
        [InlineData(TableMode.UseTempTables)]
        [InlineData(TableMode.UseRealTables)]
        public void ColumnsHasPrimaryKeyInfo(TableMode mode)
        {
            Use(mode);

            new CreateTable(new Table("Entities1", new Column("test", typeof(int)))).Execute(store);
            new AddColumn("Entities1", new Column("SomeInt", typeof(int), new SqlColumn(DbType.Int32, isPrimaryKey: true))).Execute(store);
            new AddColumn("Entities1", new Column("SomeString", typeof(string), new SqlColumn(DbType.String, isPrimaryKey: false))).Execute(store);

            var schema = store.Schema.GetSchema();

            schema["Entities1"]["SomeInt"].IsPrimaryKey.ShouldBe(true);
            schema["Entities1"]["SomeString"].IsPrimaryKey.ShouldBe(false);
        }

        [Theory]
        [InlineData(TableMode.UseTempTables)]
        [InlineData(TableMode.UseRealTables)]
        public void ColumnsHasDefaultValue(TableMode mode)
        {
            Use(mode);

            new CreateTable(new Table("Entities1", new Column("test", typeof(int)))).Execute(store);
            new AddColumn("Entities1", new Column("SomeNullableInt", typeof(int), new SqlColumn(DbType.Int32, nullable: true, defaultValue: null))).Execute(store);
            new AddColumn("Entities1", new Column("SomeOtherNullableInt", typeof(int), new SqlColumn(DbType.Int32, nullable: true, defaultValue: 42))).Execute(store);
            new AddColumn("Entities1", new Column("SomeString", typeof(string), new SqlColumn(DbType.String, defaultValue: "peter"))).Execute(store);
            new AddColumn("Entities1", new Column("SomeInt", typeof(int), new SqlColumn(DbType.Int32, defaultValue: 666))).Execute(store);

            var schema = store.Schema.GetSchema();

            schema["Entities1"]["SomeNullableInt"].DefaultValue.ShouldBe(null);
            schema["Entities1"]["SomeOtherNullableInt"].DefaultValue.ShouldBe(42);
            schema["Entities1"]["SomeString"].DefaultValue.ShouldBe("peter");
            schema["Entities1"]["SomeInt"].DefaultValue.ShouldBe(666);
        }
    }
}