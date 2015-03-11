﻿using HybridDb.Config;
using HybridDb.Migration.Commands;
using Shouldly;
using Xunit.Extensions;

namespace HybridDb.Tests
{
    public class DatabaseQuerySchemaTests : HybridDbTests
    {
        [Theory]
        [InlineData(TableMode.UseTempTables)]
        [InlineData(TableMode.UseRealTables)]
        public void ReturnsAllTables(TableMode mode)
        {
            Use(mode);

            new CreateTable(new Table("Entities1", new Column("test", typeof(int)))).Execute(database);
            new CreateTable(new Table("Entities2", new Column("test", typeof(int)))).Execute(database);

            var schema = database.QuerySchema();

            schema["Entities1"].ShouldNotBe(null);
            schema["Entities1"].Name.ShouldBe("Entities1");
            schema["Entities2"].ShouldNotBe(null);
            schema["Entities2"].Name.ShouldBe("Entities2");
        }

        [Theory]
        [InlineData(TableMode.UseTempTables)]
        [InlineData(TableMode.UseRealTables)]
        public void ReturnsAllColumns(TableMode mode)
        {
            Use(mode);

            new CreateTable(new Table("Entities1", new Column("test", typeof(int)))).Execute(database);
            new AddColumn("Entities1", new Column("SomeInt", typeof(int))).Execute(database);
            new AddColumn("Entities1", new Column("SomeBool", typeof(bool))).Execute(database);

            new CreateTable(new Table("Entities2", new Column("test", typeof(int)))).Execute(database);
            new AddColumn("Entities2", new Column("SomeString", typeof(string))).Execute(database);

            var schema = database.QuerySchema();

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

            new CreateTable(new Table("Entities1", new Column("test", typeof(int)))).Execute(database);
            new AddColumn("Entities1", new Column("SomeInt", typeof(int))).Execute(database);
            new AddColumn("Entities1", new Column("SomeBool", typeof(bool))).Execute(database);
            new AddColumn("Entities1", new Column("SomeString", typeof(string))).Execute(database);

            var schema = database.QuerySchema();

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

            new CreateTable(new Table("Entities1", new Column("test", typeof(int)))).Execute(database);
            new AddColumn("Entities1", new Column("SomeNullableInt", typeof(int?))).Execute(database);
            new AddColumn("Entities1", new Column("SomeString", typeof(string))).Execute(database);
            new AddColumn("Entities1", new Column("SomeInt", typeof(int))).Execute(database);

            var schema = database.QuerySchema();

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

            new CreateTable(new Table("Entities1", new Column("test", typeof(int)))).Execute(database);
            new AddColumn("Entities1", new Column("SomeInt", typeof(int), isPrimaryKey: true)).Execute(database);
            new AddColumn("Entities1", new Column("SomeString", typeof(string),  isPrimaryKey: false)).Execute(database);

            var schema = database.QuerySchema();

            schema["Entities1"]["SomeInt"].IsPrimaryKey.ShouldBe(true);
            schema["Entities1"]["SomeString"].IsPrimaryKey.ShouldBe(false);
        }

        [Theory]
        [InlineData(TableMode.UseTempTables)]
        [InlineData(TableMode.UseRealTables)]
        public void ColumnsHasDefaultValue(TableMode mode)
        {
            Use(mode);

            new CreateTable(new Table("Entities1", new Column("test", typeof(int)))).Execute(database);
            new AddColumn("Entities1", new Column("SomeNullableInt", typeof(int?), defaultValue: null)).Execute(database);
            new AddColumn("Entities1", new Column("SomeOtherNullableInt", typeof(int?), defaultValue: 42)).Execute(database);
            new AddColumn("Entities1", new Column("SomeString", typeof(string),  defaultValue: "peter")).Execute(database);
            new AddColumn("Entities1", new Column("SomeInt", typeof(int),  defaultValue: 666)).Execute(database);

            var schema = database.QuerySchema();

            schema["Entities1"]["SomeNullableInt"].DefaultValue.ShouldBe(null);
            schema["Entities1"]["SomeOtherNullableInt"].DefaultValue.ShouldBe(42);
            schema["Entities1"]["SomeString"].DefaultValue.ShouldBe("peter");
            schema["Entities1"]["SomeInt"].DefaultValue.ShouldBe(666);
        }
    }
}