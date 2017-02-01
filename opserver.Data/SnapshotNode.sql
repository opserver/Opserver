CREATE TABLE [dbo].[SnapshotNode]
(
	[SnapshotID] INT NOT NULL , 
	[NodeID] INT NOT NULL, 
	[Date] DATETIME NOT NULL, 
	CONSTRAINT [FK_SnapshotNode_Snapshot] FOREIGN KEY (SnapshotID) REFERENCES Snapshot(SnapshotID), 
	CONSTRAINT [FK_SnapshotNode_Node] FOREIGN KEY (NodeID) REFERENCES Node(NodeID)
)
