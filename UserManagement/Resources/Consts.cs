﻿using System.Reflection;

namespace UserManagement.Resources
{
    public static class Consts
    {
        public static class Testing
        {
            public const string IntegrationTestingEnvName = "LocalIntegrationTesting";
            public const string FunctionalTestingEnvName = "LocalFunctionalTesting";
        }

        public static class HangfireQueues
        {
            public const string Default = "default";
            public const string SyncImages = "sync-images";
            // public const string MyFirstQueue = "my-first-queue";

            public static string[] List()
            {
                return typeof(HangfireQueues)
                    .GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)
                    .Where(fi => fi.IsLiteral && !fi.IsInitOnly && fi.FieldType == typeof(string))
                    .Select(x => (string)x.GetRawConstantValue())
                    .ToArray();
            }
        }
    }
}
