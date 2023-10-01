@tool
extends EditorPlugin


func _enter_tree() -> void:
	add_custom_type("HitBox", "Area2D", preload("res://scripts/classes/HitBox.cs"), preload("res://addons/addnodes/hit_hurt_box_icon.png"))
	add_custom_type("HurtBox", "Area2D", preload("res://scripts/classes/HurtBox.cs"), preload("res://addons/addnodes/hit_hurt_box_icon.png"))


func _exit_tree() -> void:
	remove_custom_type("HitBox")
	remove_custom_type("HurtBox")
