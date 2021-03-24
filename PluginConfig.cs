﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
namespace InSongLeaderboard
{
    public class PluginConfig
    {
        public static PluginConfig Instance { get; set; }
        public bool enabled { get; set; } = true;
        public bool sortByAcc { get; set; } = true;
        public bool simpleNames { get; set; } = false;
        public Vector2 position { get; set; } = new Vector2(-6f, 2.5f);
        public float scale { get; set; } = 1f;
        public float refreshTime { get; set; } = 0.5f;
    }
}
