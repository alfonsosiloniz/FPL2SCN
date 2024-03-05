// <copyright file="ScnUtil.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace VisualPointsNamespace
{
    public class ScnUtil
    {
        public static bool HasReferenceCode(string id,  IEnumerable<KeyValuePair<string, string>> kv)
            {
                string code = id.Split(" ")[0];
                IEnumerable<string> keys = kv.Select(x => x.Key); // To get the keys.
                if (keys.Contains("PointsDefinition:" + code))
                {
                    return true;
                }

                return false;
            }
    }
}