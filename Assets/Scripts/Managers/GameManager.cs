﻿using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine.SceneManagement;
using System.IO;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public WorldManager worldManager;
    public GameObject pauseMenu;
    public GameObject gameplayUI;

    public static bool isBuildMode;
    public static byte currentBlockType = 1;

    private bool inGameScene;

    private string saveFileName = "gamesave.save";


    private void Start()
    {
        if (SceneManager.GetActiveScene().name == "GameScene")
        {
            inGameScene = true;
            Cursor.lockState = CursorLockMode.Locked;
        }
        else
            inGameScene = false;
    }

    void Update()
    {
        if (inGameScene)
        {
            if (Input.GetKeyDown(KeyCode.Escape))
                SwitchModes();

            if (Input.GetKeyDown(KeyCode.B))
                isBuildMode = !isBuildMode;

            UpdateBuildBlockType();
        }
            
    }

    /* Switch between game play and pause menu */
    private void SwitchModes()
    {
        if (Time.timeScale == 1)
        {
            HideScene();
            Cursor.lockState = CursorLockMode.Confined;
            pauseMenu.SetActive(true);
            Time.timeScale = 0;
        }
        else
        {
            UnhideScene();
            pauseMenu.SetActive(false);
            Time.timeScale = 1;
            Cursor.lockState = CursorLockMode.Locked;
        }
    }

    private void HideScene()
    {
        gameplayUI.SetActive(false);
        foreach (GameObject chunk in GameObject.FindGameObjectsWithTag("Chunk"))
            chunk.GetComponent<MeshRenderer>().enabled = false;
    }

    private void UnhideScene()
    {
        gameplayUI.SetActive(true);
        foreach (GameObject chunk in GameObject.FindGameObjectsWithTag("Chunk"))
            chunk.GetComponent<MeshRenderer>().enabled = true;
    }

    private void UpdateBuildBlockType()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            currentBlockType = 1;
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            currentBlockType = 2;
        }
        else if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            currentBlockType = 3;
        }
        else if (Input.GetKeyDown(KeyCode.Alpha4))
        {
            currentBlockType = 4;
        }
    }

    public void StartGame()
    {
        SceneManager.LoadScene("GameScene");
    }

    /* Save current game state to file */
    public void SaveGame()
    {
        SaveData save = new SaveData();

        for (int y = WorldManager.numChunks - 1; y >=0; y--)
            for (int x = 0; x < WorldManager.numChunks; x++)
                for (int z = 0; z < WorldManager.numChunks; z++)
                {
                    ChunkData chunkData = new ChunkData();
                    Vector3S position = new Vector3S(x, y, z);

                    for (int j = WorldManager.chunkSize - 1; j >= 0; j--)
                        for (int i = 0; i < WorldManager.chunkSize; i++)
                            for (int k = 0; k < WorldManager.chunkSize; k++)
                            {
                                byte type = worldManager.chunks[x, y, z].chunkGrid[i, j, k];
                                if (type != 0)
                                {
                                    // store only occupied blocks
                                    chunkData.blockPositions.Add(new Vector3S(i, j, k));
                                    chunkData.blockTypes.Add(type);
                                }
                            }
                    // chunk stored in dictionary with its position as key
                    save.chunks.Add(position, chunkData);
                }

        // store player's position
        Vector3 playerPos = GameObject.Find("Player").transform.position;
        save.playerPosition = new Vector3S((int)playerPos.x, (int)playerPos.y, (int)playerPos.z);

        Debug.Log("Saving game state to: " + Application.persistentDataPath);
        BinaryFormatter bf = new BinaryFormatter();
        FileStream file = File.Create(Application.persistentDataPath + "/" + saveFileName);
        bf.Serialize(file, save);
        file.Close();

        SwitchModes();
    }

    /* Load saved game state from file */
    public void LoadGame()
    {
        if (File.Exists(Application.persistentDataPath + "/" + saveFileName))
        {
            // clear current world
            worldManager.EmptyWorld();

            BinaryFormatter bf = new BinaryFormatter();
            FileStream file = File.Open(Application.persistentDataPath + "/" + saveFileName, FileMode.Open);
            SaveData save = (SaveData)bf.Deserialize(file);
            file.Close();

            // recreate chunks
            foreach (KeyValuePair<Vector3S, ChunkData> item in save.chunks)
            {
                Vector3S chunkPos = item.Key;
                ChunkData chunkData = item.Value;

                worldManager.chunks[chunkPos.x, chunkPos.y, chunkPos.z].ReCreateChunkFromSave(ref chunkData, chunkPos);
            }

            // position player
            GameObject.FindWithTag("Player").transform.position = new Vector3(save.playerPosition.x, save.playerPosition.y, save.playerPosition.z) + Vector3.one * 0.5f;

            Debug.Log("Loading successful");
        }
        else
            Debug.Log("Loading unsuccessful");

        SwitchModes();
    }

    public void RestartGame()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        Time.timeScale = 1;
    }
    public void Exit()
    {
        Application.Quit();
    }
}
