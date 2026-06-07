@tool
extends EditorScript

func _run():
    var zip_path = _select_zip()
    if zip_path.is_empty():
        return
    _import_zip(zip_path)

func _select_zip():
    var dialog = EditorFileDialog.new()
    dialog.file_mode = EditorFileDialog.FILE_MODE_OPEN_FILE
    dialog.add_filter("*.zip", "ZIP Archive")
    EditorInterface.get_base_control().add_child(dialog)
    dialog.popup_centered()
    var result = await dialog.file_selected
    dialog.queue_free()
    return result

func _import_zip(zip_path):
    var temp_dir = "user://temp_pbr/"
    var dir = DirAccess.open("user://")
    if dir.dir_exists(temp_dir):
        dir.delete(temp_dir)
    dir.make_dir(temp_dir)
    
    var zip = ZIPReader.new()
    zip.open(zip_path)
    var files = zip.get_files()
    for f in files:
        var data = zip.read_file(f)
        var out_path = temp_dir + f
        var out_dir = out_path.get_base_dir()
        if not dir.dir_exists(out_dir):
            dir.make_dir_recursive(out_dir)
        var file = FileAccess.open(out_path, FileAccess.WRITE)
        file.store_buffer(data)
        file.close()
    zip.close()
    
    # Определяем карты (как в полной версии)
    var tex_paths = {}
    for f in files:
        var name = f.get_basename().to_lower()
        if "basecolor" in name or "albedo" in name:
            tex_paths["basecolor"] = temp_dir + f
        elif "normal" in name:
            tex_paths["normal"] = temp_dir + f
        elif "roughness" in name:
            tex_paths["roughness"] = temp_dir + f
        elif "metallic" in name:
            tex_paths["metallic"] = temp_dir + f
        elif "height" in name:
            tex_paths["height"] = temp_dir + f
        elif "ao" in name:
            tex_paths["ao"] = temp_dir + f
    
    if not tex_paths.has("basecolor"):
        _show_error("No base color map found")
        return
    
    var target_dir = "res://PatternForgeTextures/"
    if not DirAccess.dir_exists_absolute(target_dir):
        DirAccess.make_dir_absolute(target_dir)
    
    for key in tex_paths:
        var src = tex_paths[key]
        var dst = target_dir + key + ".png"
        var file = FileAccess.open(src, FileAccess.READ)
        var data = file.get_buffer(file.get_length())
        file.close()
        var out = FileAccess.open(dst, FileAccess.WRITE)
        out.store_buffer(data)
        out.close()
    
    EditorInterface.get_resource_filesystem().scan()
    _show_message("Textures imported to " + target_dir)

func _show_message(msg):
    var dialog = AcceptDialog.new()
    dialog.dialog_text = msg
    EditorInterface.get_base_control().add_child(dialog)
    dialog.popup_centered()

func _show_error(msg):
    var dialog = AcceptDialog.new()
    dialog.dialog_text = msg
    EditorInterface.get_base_control().add_child(dialog)
    dialog.popup_centered()
