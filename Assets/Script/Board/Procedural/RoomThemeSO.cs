using System;
using System.Collections.Generic;
using UnityEngine;

namespace HeroQuest.Board
{
    /// <summary>Where a prop is allowed to go inside a room.</summary>
    public enum PropSlot
    {
        Wall,   // cell touching at least one wall: the prop leans against it, facing the room
        Corner, // cell touching two perpendicular walls
        Floor   // any free cell
    }

    [Serializable]
    public class PropEntry
    {
        public GameObject prefab;
        public PropSlot slot = PropSlot.Wall;

        [Tooltip("Largest dimension of the prop, in CELLS (0.4 = 40% of a cell). The prop is rescaled automatically, so the pack's native scale does not matter.")]
        [Range(0.05f, 1f)] public float size = 0.4f;

        [Tooltip("Relative chance to be picked.")]
        public float weight = 1f;

        [Tooltip("Floor props only: random rotation instead of a fixed one.")]
        public bool randomYaw = true;
    }

    /// <summary>
    /// A decoration theme: which props, how dense, which lights.
    /// PURELY COSMETIC data. A theme never changes a rule: no collider, no occupied cell, no gameplay effect.
    /// </summary>
    [CreateAssetMenu(menuName = "HeroQuest/Decor/Room Theme")]
    public class RoomThemeSO : ScriptableObject
    {
        public string themeId = "crypt";

        [Header("Props")]
        public List<PropEntry> props = new List<PropEntry>();
        [Range(0f, 0.5f)] public float density = 0.15f;   // share of free cells that receive a prop
        public int minProps = 2;
        public int maxProps = 8;

        [Header("Wall torches")]
        [Tooltip("Wall-mounted torch model (optional: without it, only the light is created).")]
        public GameObject torchPrefab;
        [Tooltip("Optional FX (flame, sparks) from your fx folder, spawned at the top of the torch.")]
        public GameObject flameFxPrefab;
        public int minTorches = 1;
        public int maxTorches = 3;
        [Range(0.05f, 1f)] public float torchSize = 0.35f;
        [Tooltip("Height of the torch bottom, as a ratio of the wall height.")]
        [Range(0f, 1f)] public float torchHeight = 0.5f;
        public Color torchColor = new Color(1f, 0.62f, 0.25f);
        public float torchIntensity = 1.6f;
        [Tooltip("In cells.")] public float torchRange = 3.5f;
        public int torchMinSpacing = 3;                   // in cells (Manhattan) between two torches of the same room

        [Header("Fill light (one soft light per room)")]
        public float fillIntensity = 0.6f;                // 0 = disabled
        public Color fillColor = new Color(0.8f, 0.75f, 0.7f);
    }
}