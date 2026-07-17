namespace _Shmackle.Scripts.Player
{
	[System.Serializable]
	public struct BlendShapeData
	{
		public BlendShapeType BlendShapeType;
		public int            BlendShapeValue;
	}

	public enum BlendShapeType //For Body
	{
		Talking   = 0,
		Blinking  = 1,
		TeethShow = 2,
		NerdFace  = 3,
		Belly     = 4,
		Kiss = 5,
		Transformation = 6,
	}

	public enum BlendShapeTransformationType // For other part
	{
		Transformation = 0,
	}

	public enum BlendShapePartType
	{
		PartIndex0 = 0,
		PartIndex1 = 1,
	}
    
	//NECK
	[System.Serializable]
	public struct BlendShapeNeckData
	{
		public BlendShapeNeckType BlendShapeType;
		public int                BlendShapeValue;
	}

	public enum BlendShapeNeckType //For Neck
	{
		Key_1   = 0,
	}
    
	//BELT
	[System.Serializable]
	public struct BlendShapeBeltData
	{
		public BlendShapeBeltType BlendShapeType;
		public int                BlendShapeValue;
	}

	public enum BlendShapeBeltType //For Neck
	{
		MiniGame   = 0,
	}
	
	//BADGE
	[System.Serializable]
	public struct BlendShapeBadgeData
	{
		public BlendShapeBadgeType BlendShapeType;
		public int                BlendShapeValue;
	}

	public enum BlendShapeBadgeType //For Neck
	{
		Key_1   = 0,
	}
}