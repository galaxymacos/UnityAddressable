using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class ParticleSpawnerButton : MonoBehaviour
{
    [SerializeField] private int _index;

    [SerializeField] private int _spawnCount = 1;

    public void RequestSpawnParticle()
    {
        for (int i = 0; i < _spawnCount; i++)
        {
            FindObjectOfType<ParticleSpawner>().Spawn(_index);
        }
    }

    private void OnValidate()
    {
        _index = transform.GetSiblingIndex();
        GetComponentInChildren<TextMeshProUGUI>().text = _index.ToString();
    }
    
    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha0 + _index))
        {
            RequestSpawnParticle();
        }
    }
}
