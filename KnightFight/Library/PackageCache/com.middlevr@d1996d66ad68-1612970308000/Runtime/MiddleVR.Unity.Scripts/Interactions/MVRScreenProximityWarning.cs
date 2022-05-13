/* MVRInteractionScreenProximityWarning
 * MiddleVR
 * (c) MiddleVR
 */

using System.Collections.Generic;
using MiddleVR;
using UnityEngine;

public class MVRScreenProximityWarning : MonoBehaviour
{
    public float WarningDistance = 0.4f;
    public List<string> NodesToWatch;
    public GameObject WarningRepresentationPrefab;

    private bool _initialized = false;
    private float _textureScale = 2.5f;

    private List<GameObject> _nodes;
    private List<GameObject> _screens;

    private List<Contact> _contacts;
    private List<Contact> _addedContacts;
    private List<Contact> _removedContacts;

    private readonly Dictionary<string, GameObject> _warningRepresentationObjects = new Dictionary<string, GameObject>();

    private class Contact
    {
        public Contact(GameObject iNode, GameObject iScreen)
        {
            Node = iNode;
            Screen = iScreen;
        }

        ~Contact()
        {


        }

        public virtual bool Equals(Contact iContact)
        {
            if (iContact.Node == Node && iContact.Screen == Screen)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public GameObject Node;
        public GameObject Screen;
    }

    // Start is called before the first frame update
    void Start()
    {
        _nodes = new List<GameObject>();
        _screens = new List<GameObject>();

        _contacts = new List<Contact>();
        _addedContacts = new List<Contact>();
        _removedContacts = new List<Contact>();
    }

    bool Init()
    {
        _initialized = true;

        if (NodesToWatch.Count <= 0)
        {
            return true;
        }

        // Cache nodes
        foreach (var name in NodesToWatch)
        {
            GameObject node = GameObject.Find(name);
            if (node != null)
            {
                _nodes.Add(node);
            }
            else
            {
                _initialized = false;
            }
        }

        // Cache screens
        foreach (var mvrScreen in MVR.DisplayMgr.Screens)
        {
            GameObject screen = GameObject.Find(mvrScreen.GetName());

            if (screen != null)
            {
                _screens.Add(screen);
            }
            else
            {
                _initialized = false;
            }
        }

        return _initialized;
    }

    // Update is called once per frame
    void Update()
    {
        if (!_initialized)
        {
            if (!Init())
            {
                return;
            }
        }

        foreach (var node in _nodes)
        {
            foreach (var screen in _screens)
            {
                Contact newContact = new Contact(node, screen);

                bool contactExists = false;

                foreach (var existingContact in _contacts)
                {
                    if (existingContact == newContact) contactExists = true;
                }

                if (IsNodeCloseToScreen(node, screen, WarningDistance))
                {
                    if (!contactExists)
                    {
                        // Close and not in close list
                        _contacts.Add(newContact);
                        _addedContacts.Add(newContact);
                    }
                }
                else
                {
                    if (contactExists)
                    {
                        // Not close and in close list
                        _contacts.Remove(newContact);
                        _removedContacts.Add(newContact);
                    }
                }
            }
        }

        HideOldWarnings();
        ShowNewWarnings();

        _addedContacts.Clear();
        _removedContacts.Clear();
    }

    private bool IsNodeCloseToScreen(GameObject iNode, GameObject iScreen, float warningDistance)
    {
        bool isClose = false;

        Vector3 relPos = iScreen.transform.InverseTransformPoint(iNode.transform.position);

        vrScreen screen = MVR.DisplayMgr.GetScreen(iScreen.name);

        if (Mathf.Abs(relPos.z) <= warningDistance)
        {
            if (Mathf.Abs(relPos.x) <= (warningDistance + screen.GetWidth() / 2.0f) &&
                Mathf.Abs(relPos.z) <= (warningDistance + screen.GetHeight() / 2.0f))
            {
                return true;
            }
        }

        return isClose;
    }

    private void CreateWarningRepresentation(GameObject iNode, GameObject iScreen)
    {
        // Generate name and check if warning doesn't exist
        string warningName = iNode.name + "_" + iScreen.name + "_ProxiWarning";
        if (_warningRepresentationObjects.ContainsKey(warningName))
        {
            return;
        }

        // Retrieve the GameObjects
        vrNode3D node = MVR.DisplayMgr.GetNode(iNode.name);
        vrScreen screen = MVR.DisplayMgr.GetScreen(iScreen.name);

        // Create and initialize the warning
        GameObject warningRepresentation = GameObject.Instantiate(WarningRepresentationPrefab) as GameObject;
        warningRepresentation.name = warningName;
        warningRepresentation.transform.parent = iScreen.transform;
        warningRepresentation.transform.localPosition = Vector3.zero;
        warningRepresentation.transform.localRotation = Quaternion.identity;
        warningRepresentation.transform.localScale = new Vector3(screen.GetWidth(), screen.GetHeight(), 1.0f);

        GameObject warningMesh = warningRepresentation.transform.GetChild(0).gameObject;
        Material mat = Material.Instantiate(warningMesh.GetComponent<Renderer>().material) as Material;
        warningMesh.GetComponent<Renderer>().material = mat;
        warningMesh.GetComponent<Renderer>().material.SetTextureScale("_MainTex", new Vector2(_textureScale * 0.8f * screen.GetWidth(), _textureScale * screen.GetHeight()));

        MVRScreenWarningAnimation warningScript = warningMesh.GetComponent<MVRScreenWarningAnimation>();
        warningScript.SetNodeToWatch(iNode);
        warningScript.SetNearDistance(WarningDistance);

        // Add the warning to list
        _warningRepresentationObjects[warningRepresentation.name] = warningRepresentation;
    }

    private void DeleteWarningRepresentation(GameObject iNode, GameObject iScreen)
    {
        // Generate name and check if warning exists
        string warningName = iNode.name + "_" + iScreen.name + "_ProxiWarning";
        if (!_warningRepresentationObjects.ContainsKey(warningName))
        {
            return;
        }

        // Remove from list
        _warningRepresentationObjects.Remove(warningName);

        // Destroy warning object
        GameObject warningObject = GameObject.Find(warningName);
        warningObject.SetActive(false);
        GameObject.Destroy(warningObject);
    }

    private void ShowNewWarnings()
    {
        foreach (var contact in _addedContacts)
        {
            CreateWarningRepresentation(contact.Node, contact.Screen);
        }
    }

    private void HideOldWarnings()
    {
        foreach (var contact in _removedContacts)
        {
            DeleteWarningRepresentation(contact.Node, contact.Screen);
        }
    }

    protected void OnDisable()
    {
        // Delete all representations
        foreach (KeyValuePair<string, GameObject> warningRepresentation in _warningRepresentationObjects)
        {
            GameObject.Destroy(warningRepresentation.Value);
        }
        _warningRepresentationObjects.Clear();
    }
}
