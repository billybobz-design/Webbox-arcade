using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Webbox.Training
{
    [CustomEditor(typeof(FighterSpritePack))]
    internal sealed class FighterSpritePackEditor : Editor
    {
        private const float ReferenceHeight = 71f / 30f;
        private static readonly string[] FrameFields =
        {
            "idleFrames", "walkFrames", "guardFrames", "crouchFrames", "jumpFrames", "landFrames",
            "windupFrames", "activeFrames", "recoveryFrames",
            "hookWindupFrames", "hookActiveFrames", "hookRecoveryFrames",
            "heavyWindupFrames", "heavyActiveFrames", "heavyRecoveryFrames"
        };

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            EditorGUILayout.HelpBox(
                "Uses the first Idle frame to match the training fighter's visible height. " +
                "Applies pixel-art import settings to every PNG used by this pack. " +
                "Set the foot pivots in Sprite Editor before applying. Shared PNGs affect every pack that uses them.",
                MessageType.Info);

            if (!GUILayout.Button("Match Arcade Sprite Scale"))
                return;

            try
            {
                float pixelsPerUnit = ApplyArcadeSettings((FighterSpritePack)target);
                Debug.Log($"Applied arcade settings at {pixelsPerUnit:0.###} pixels per unit.", target);
            }
            catch (Exception exception) when (exception is InvalidOperationException || exception is IOException)
            {
                EditorUtility.DisplayDialog("Arcade Sprite Scale", exception.Message, "OK");
            }
        }

        internal static float ApplyArcadeSettings(FighterSpritePack pack)
        {
            var data = new SerializedObject(pack);
            SerializedProperty idleFrames = data.FindProperty(FrameFields[0]);
            if (idleFrames.arraySize == 0 || idleFrames.GetArrayElementAtIndex(0).objectReferenceValue is not Sprite idle)
                throw new InvalidOperationException("Assign the first Idle frame before matching scale.");

            var paths = new HashSet<string>();
            foreach (string field in FrameFields)
            {
                SerializedProperty frames = data.FindProperty(field);
                for (int index = 0; index < frames.arraySize; index++)
                {
                    if (frames.GetArrayElementAtIndex(index).objectReferenceValue is Sprite frame)
                        paths.Add(AssetDatabase.GetAssetPath(frame));
                }
            }
            if (data.FindProperty("airShadow").objectReferenceValue is Sprite shadow)
                paths.Add(AssetDatabase.GetAssetPath(shadow));

            var importers = new List<TextureImporter>();
            foreach (string path in paths)
            {
                if (!path.StartsWith("Assets/", StringComparison.Ordinal) ||
                    !path.EndsWith(".png", StringComparison.OrdinalIgnoreCase) ||
                    AssetImporter.GetAtPath(path) is not TextureImporter importer)
                    throw new InvalidOperationException($"Use PNG sprites under Assets for this operation. Unsupported asset: {path}");

                importers.Add(importer);
            }

            float pixelsPerUnit = MeasureIdleHeight(idle) / ReferenceHeight;
            foreach (TextureImporter importer in importers)
            {
                importer.spritePixelsPerUnit = pixelsPerUnit;
                importer.filterMode = FilterMode.Point;
                importer.mipmapEnabled = false;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.crunchedCompression = false;
                importer.SaveAndReimport();
            }

            return pixelsPerUnit;
        }

        private static int MeasureIdleHeight(Sprite idle)
        {
            var source = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            try
            {
                string path = AssetDatabase.GetAssetPath(idle);
                if (!ImageConversion.LoadImage(source, File.ReadAllBytes(path)))
                    throw new InvalidOperationException("Could not read the first Idle PNG.");

                if (source.width != idle.texture.width || source.height != idle.texture.height)
                    throw new InvalidOperationException(
                        "Import the first Idle PNG at its original resolution before matching scale.");

                Rect rect = idle.rect;
                Color32[] pixels = source.GetPixels32();
                int first = (int)rect.yMax;
                int last = -1;
                for (int y = (int)rect.yMin; y < (int)rect.yMax; y++)
                {
                    for (int x = (int)rect.xMin; x < (int)rect.xMax; x++)
                    {
                        if (pixels[y * source.width + x].a != 255)
                            continue;

                        first = Mathf.Min(first, y);
                        last = y;
                        break;
                    }
                }

                if (last < first)
                    throw new InvalidOperationException("The first Idle frame must contain fully opaque character pixels.");

                return last - first + 1;
            }
            finally
            {
                DestroyImmediate(source);
            }
        }
    }
}
