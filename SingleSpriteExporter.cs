using UnityEngine;
using UnityEditor;
using System.IO;

namespace SpriteExporter
{
    public class SingleSpriteExporter
    {
        [MenuItem("Assets/SpriteExporter/Export Sprites")]
        public static void ExportSprites()
        {
            var selectedObjects = Selection.objects;
            if (selectedObjects == null || selectedObjects.Length == 0)
            {
                Debug.LogError("未选择任何精灵图！请在项目视图中选择精灵图。");
                return;
            }

            int successCount = 0;
            int spriteCount = 0;

            foreach (var obj in selectedObjects)
            {
                if (obj is Sprite sprite)
                {
                    spriteCount++;
                    if (ExportSprite(sprite))
                    {
                        successCount++;
                    }
                }
            }

            if (spriteCount == 0)
            {
                Debug.LogError("选中的对象中没有精灵图！请确保选择的是精灵图资源。");
                return;
            }

            string message = spriteCount == 1 
                ? $"精灵图导出{(successCount > 0 ? "成功" : "失败")}" 
                : $"批量导出完成：成功导出 {successCount} 个精灵图，共选择 {spriteCount} 个精灵图。";
            
            Debug.Log(message);
        }

        private static bool ExportSprite(Sprite sprite)
        {
            Texture2D texture = sprite.texture;

            if (texture == null)
            {
                Debug.LogError($"无法加载精灵图 {sprite.name} 的纹理。");
                return false;
            }

            // 检查并修改纹理的可读性
            string texturePath = AssetDatabase.GetAssetPath(texture);
            TextureImporter textureImporter = AssetImporter.GetAtPath(texturePath) as TextureImporter;
            bool needsReimport = false;

            if (textureImporter != null && !textureImporter.isReadable)
            {
                textureImporter.isReadable = true;
                needsReimport = true;
                AssetDatabase.ImportAsset(texturePath);
                texture = sprite.texture; // 重新获取纹理，因为之前的引用可能已经失效
            }

            // 获取原始 Sprite 的路径
            string spritePath = AssetDatabase.GetAssetPath(sprite);
            string directory = Path.GetDirectoryName(spritePath);
            string savePath = Path.Combine(directory, sprite.name + ".png");

            // Create a new texture for the selected sprite
            Texture2D newTexture = new Texture2D((int)sprite.rect.width, (int)sprite.rect.height);
            Color[] pixels = texture.GetPixels((int)sprite.rect.x, (int)sprite.rect.y, (int)sprite.rect.width, (int)sprite.rect.height);
            newTexture.SetPixels(pixels);
            newTexture.Apply();

            // Convert the texture to PNG
            byte[] pngData = newTexture.EncodeToPNG();
            bool success = false;
            
            if (pngData != null)
            {
                string fullPath = Path.GetFullPath(savePath);
                File.WriteAllBytes(fullPath, pngData);
                Object.DestroyImmediate(newTexture);
                Debug.Log($"精灵图导出成功：{savePath}");
                success = true;
            }
            else
            {
                Debug.LogError($"无法将精灵图 {sprite.name} 编码为 PNG。");
            }

            // 如果之前修改了导入设置，现在改回去
            if (needsReimport && textureImporter != null)
            {
                textureImporter.isReadable = false;
                AssetDatabase.ImportAsset(texturePath);
            }

            if (success)
            {
                AssetDatabase.Refresh();
            }
            return success;
        }
    }
}