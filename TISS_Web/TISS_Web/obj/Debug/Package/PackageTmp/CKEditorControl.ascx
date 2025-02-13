<%@ Control Language="C#" AutoEventWireup="true" CodeBehind="CKEditorControl.ascx.cs" Inherits="TISS_Web.CKEditorControl" %>
<textarea name="ckeditor-content" id="ckeditor-content"></textarea>
<script>
    CKEDITOR.replace('ckeditor-content');
</script>