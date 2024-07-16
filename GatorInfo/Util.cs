using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GatorInfo;

public static class Util {
     public static GameObject GetByPath(string path)
    {
        var elements = path.Trim('/').Split('/');
        var activeScene = SceneManager.GetActiveScene();
        var rootObjects = activeScene.GetRootGameObjects();

        var root = rootObjects.First((go) => go.name == elements[0]);
        GameObject current = root;
        foreach (var element in elements.Skip(1))
        {
            current = current.transform.Cast<Transform>()
            .First((t) => t.name == element)
            .gameObject;
        }
        return current;
    }
}