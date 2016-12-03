using IOUtils = KSP.IO.IOUtils;
using Altimeter = KSP.UI.Screens.Flight.AltitudeTumbler;

using UnityEngine;
using UnityEngine.EventSystems;

/* How this works: There is one class, the AltDrag component, that implements
 * drag behavior for the altimeter. The second class, the Attacher, spawns when
 * flight starts and adds the component to the altimeter. I can't work out how
 * to make the dragger attach itself. I feel like there should be a cleaner way
 * to do this but I haven't found it yet.
 */

[KSPAddon(KSPAddon.Startup.Flight, false)]
public class Attacher : MonoBehaviour
{
	void Start()
	{
		/* Rather than only dragging the altimeter, we really want to
		 * drag the entire control cluster it's part of.  So the
		 * dragger gets attached to that. It's the grandparent of the
		 * altimeter object. (Found this using DebugStuff for the
		 * curious)
		 */
          
		GameObject altimeter = GameObject.Find("Altimeter");
		GameObject slideframeparent = altimeter.transform.parent.gameObject;
		GameObject cluster = slideframeparent.transform.parent.gameObject;
		cluster.AddComponent<Dragger>();
		Destroy(gameObject);
	}
}
public class Dragger : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    /* This makes the altimeter draggable. Movement is restricted to the x
	 * axis, so it slides along the edge of the screen.
	 */

    /* I can't figure out how to make ConfigNode.Load do the right thing
	 * with C# properties, so ALTIMETER_XCOORD gets mapped to a private
	 * variable that serves just as a place to keep save-load data.
	 */
    [Persistent]
    private float ALTIMETER_XCOORD;

    private Vector2 dragstart;
	private Vector3 altstart;

	const string cfgfile = "DraggableAltimeter.cfg";

	void Start()
	{
		string path = IOUtils.GetFilePathFor(this.GetType(), cfgfile);
		ConfigNode config = ConfigNode.Load(path);
		ConfigNode.LoadObjectFromConfig(this, config);
		xpos = ALTIMETER_XCOORD;
    }


    public float xpos
	{
		/* Assigning to xpos moves the altimeter. Don't do it any other
		 * way; stuff necessary for consistency happens here.
		 */
		get { return transform.position.x; }
		set
		{
			// Prevent the altimeter from ending up offscreen where
			// we can't drag it back.
			int right_edge = GameSettings.SCREEN_RESOLUTION_WIDTH / 2;
			int left_edge  = -right_edge;
			if (value > right_edge) value = right_edge;
			if (value < left_edge)  value = left_edge;

			// Move the altimeter.
			Vector3 newpos = transform.position;
			newpos.x = value;
			ALTIMETER_XCOORD = value;
			transform.position = newpos;
		}
	}

    public void OnBeginDrag(PointerEventData evtdata)
	{
		/* Record the pointer and altimeter positions when dragging
		 * starts.  We'll need them in OnDrag. */
		dragstart = evtdata.position;
		altstart = transform.position;
	}
	public void OnDrag(PointerEventData evtdata)
	{
		/* See how far the pointer has dragged and offset the altimeter
		 * from its original position by that much.
		 *
		 * This might be cleaner with a frame-by-frame delta but I
		 * worry about small errors adding up.
		 */
		Vector2 dragdist = evtdata.position - dragstart;
		xpos = altstart.x + dragdist.x;
	}

    public void Update()
	{
        xpos = ALTIMETER_XCOORD;
    }

    public void OnEndDrag(PointerEventData evtdata)
	{
		ALTIMETER_XCOORD = xpos;
		ConfigNode config = ConfigNode.CreateConfigFromObject(this);
		string path = IOUtils.GetFilePathFor(this.GetType(), cfgfile);
		config.Save(path);
	}
}
