using System.Linq;
using System.Reflection;
using System.Collections;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using BaseApi.Models;
using Toolbelt.ComponentModel.DataAnnotations;
using System;
using System.Collections.Generic;

//EXTEND THIS FOR YOUR DB Context

namespace BaseApi.Storage
{
    public class TemplateMicrosoftSQLDbContext : DbContext
    {
        public TemplateMicrosoftSQLDbContext(DbContextOptions options) : base(options)
        {

        }

        protected virtual bool ConvertEnumsToString
        {
            get
            {
                return true;
            }
        }

        protected virtual IEnumerable<string> ExcludedEnumToStringTypeNames
        {
            get
            {
                return null;
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.BuildIndexesFromAnnotationsForSqlServer();

            //Auto generates tables that are of base type, since normally extending a base type results in many tables(base table and each extended) or in one super table
            var publicPropertieBaseTypes = GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance)
                        .Where(p => p.PropertyType.IsGenericType &&
                                    p.PropertyType.GetGenericTypeDefinition() == typeof(DbSet<>) &&
                                    p.PropertyType.GetGenericArguments()[0].IsSubclassOf(typeof(BaseModel)))
                        .Select(p => p.PropertyType.GetGenericArguments()[0]);

            //Setup default values for base class while maintaining individual tables
            //Creates the base table ignoring other tables including attr and totable
            Type enumToStringGenericType = typeof(EnumToStringConverter<>);
            foreach (var publicPropertieBaseType in publicPropertieBaseTypes)
            {
                if (ConvertEnumsToString)
                {
                    //Loop through the child properties querying for enums to convert to string form
                    foreach (var childProperties in publicPropertieBaseType.GetProperties())
                    {
                        if (childProperties.GetType().IsEnum)
                        {
                            if (ExcludedEnumToStringTypeNames != null && ExcludedEnumToStringTypeNames.Contains(childProperties.GetType().Name))
                            {
                                continue;
                            }

                            var enumToStringType = enumToStringGenericType.MakeGenericType(childProperties.GetType());
                            var enumToStringInstance = Activator.CreateInstance(enumToStringType);

                            modelBuilder.Entity(publicPropertieBaseType)
                                                .Property(childProperties.Name)
                                                .HasConversion(enumToStringInstance as ValueConverter)
                                                .HasMaxLength(128);
                        }
                    }
                }

                modelBuilder.Entity(publicPropertieBaseType, c =>
                {
                    c.Property("DateCreated").HasDefaultValueSql("GETUTCDATE()").ValueGeneratedOnAdd();
                    c.Property("DateModified").HasDefaultValueSql("GETUTCDATE()").ValueGeneratedOnAddOrUpdate();
                });
            }
        }
    }
}
