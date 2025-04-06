using UnityEngine;
using System.Collections;

public class LastPosition : MonoBehaviour
{
	public GameObject lastPosition; // Prefab with a trigger collider
	private Vector3 prevPos; // Player position
	
	// Last Position spawn rate variables
	private float posNext = 0.0f;
	private float posRate = 0.25f;

	void Update()
	{	
		if(Time.time > posNext)	// If the current time is greater than the posNext float then continue
		{
			posNext = Time.time + posRate;	// Increase the posNext float by Time.time plus the posRate
			if(transform.position != prevPos)	// If your current position is not equal to your previous position then continue 
			{
				Instantiate(lastPosition,transform.position,Quaternion.identity); // Create a GameObject prefab at the current location of the player
			}
		}
		prevPos = transform.position;	// Set the prevPos Vector3 to equal your current position
	}
}