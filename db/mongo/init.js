// MongoDB — dms_metadata init
db = db.getSiblingDB('dms_metadata');

db.createCollection('file_tags');

db.file_tags.createIndex(
    { title: "text", code: "text", category_name: "text", department_name: "text" },
    { name: "dms_master_index", default_language: "spanish" }
);
