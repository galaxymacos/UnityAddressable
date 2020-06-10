using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using Random = UnityEngine.Random;

public class ParticleSpawner : MonoBehaviour
{
    [SerializeField] private List<AssetReference> _particleReferences;
    // Start is called before the first frame update
    
    private readonly Dictionary<AssetReference, List<GameObject>> _spawnedParticleSystems = new Dictionary<AssetReference, List<GameObject>>();

    // The queue holds requests to spawn an instanced that were made while we are already loading the asset
    // They are spawned once the addressable is loaded, in the order requested
    private readonly Dictionary<AssetReference, Queue<Vector3>> _queuedSpawnRequests = new Dictionary<AssetReference, Queue<Vector3>>();
    
    private readonly Dictionary<AssetReference, AsyncOperationHandle<GameObject>> _asyncOperationHandles = new Dictionary<AssetReference, AsyncOperationHandle<GameObject>>();

    public void Spawn(int index)
    {

        if (index < 0 || index >= _particleReferences.Count)
        {
            return;
        }
        
        AssetReference assetReference = _particleReferences[index];
        if (!assetReference.RuntimeKeyIsValid())
        {
            Debug.Log("Invalid Key "+assetReference.RuntimeKey.ToString());
            return;
        }

        if (_asyncOperationHandles.ContainsKey(assetReference))
        {
            if (_asyncOperationHandles[assetReference].IsDone)
            {
                SpawnParticleFromLoadedReference(assetReference, GetRandomPosition());
            }
            else
            {
                EnqueueSpawnForAfterAInitialization(assetReference);
                return;
            }
            
        }
        LoadAndSpawn(assetReference);

    }

    private void EnqueueSpawnForAfterAInitialization(AssetReference assetReference)
    {
        if(!_queuedSpawnRequests.ContainsKey(assetReference))
        {
            _queuedSpawnRequests[assetReference] = new Queue<Vector3>();
        }
        _queuedSpawnRequests[assetReference].Enqueue(GetRandomPosition());
    }

    private void SpawnParticleFromLoadedReference(AssetReference assetReference, Vector3 position)
    {
        assetReference.InstantiateAsync(position, Quaternion.identity).Completed += (asyncOperationHandle) =>
        {
            if (!_spawnedParticleSystems.ContainsKey(assetReference))
            {
                _spawnedParticleSystems[assetReference] = new List<GameObject>();
            }

            _spawnedParticleSystems[assetReference].Add(asyncOperationHandle.Result);    // Add the actually game object to the list
            var notify = asyncOperationHandle.Result.AddComponent<NotifyOnDestroy>();
            notify.Destroyed += Remove;
            notify.AssetReference = assetReference;
        };
    }


    private void LoadAndSpawn(AssetReference assetReference)
    {
        print("Load and spawn");

        var op = Addressables.LoadAssetAsync<GameObject>(assetReference);
        _asyncOperationHandles[assetReference] = op;
        op.Completed += (operation) =>
        {
            SpawnParticleFromLoadedReference(assetReference, GetRandomPosition());
            if (_queuedSpawnRequests.ContainsKey(assetReference))
            {
                while (_queuedSpawnRequests[assetReference]?.Any() == true)
                {
                    var position = _queuedSpawnRequests[assetReference].Dequeue();
                    SpawnParticleFromLoadedReference(assetReference, position);
                }
            }
        };
        print("Load and spawn");
    }

    private void EnqueueSpawnForAfterInitialization(AssetReference assetReference)
    {
        if (_queuedSpawnRequests.ContainsKey(assetReference) == false)
        {
            _queuedSpawnRequests[assetReference] = new Queue<Vector3>();
        }
        _queuedSpawnRequests[assetReference].Enqueue(GetRandomPosition());
    }

    private Vector3 GetRandomPosition()
    {
        return new Vector3(Random.Range(-5, 5), 1, Random.Range(-5, 5));
    }

    private void Remove(AssetReference assetReference, NotifyOnDestroy obj)
    {
        // 释放 用 addressable 方法生成的 game object
        Addressables.ReleaseInstance(obj.gameObject);

        _spawnedParticleSystems[assetReference].Remove(obj.gameObject);
        if (_spawnedParticleSystems[assetReference].Count == 0)
        {
            Debug.Log($"Removed all {assetReference.RuntimeKey}");

            if (_asyncOperationHandles[assetReference].IsValid())
            {
                // 释放异步句柄
                Addressables.Release(_asyncOperationHandles[assetReference]);
            }
            _asyncOperationHandles.Remove(assetReference);
        }
    }
    
    

}