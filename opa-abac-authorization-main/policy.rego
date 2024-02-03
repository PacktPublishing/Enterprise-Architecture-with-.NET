package app.abac

import future.keywords
import data.org_chart

# =========================== TODO : traiter le cas de l'id de l'auteur répété dans la clé de data et dans le contenu JSON (et idem pour les livres)
# =========================== TODO : mettre des URN à la place des identifiants simples

###################
# GENERAL DECISION
###################

# In order to implement security best practices, access is forbidden by default
# and, for it to be allowed, the action will have to satisfy each rule needed
default allow := false
allow if {
	permission_associated_to_role
	no_author_blocking
	no_status_blocking
	authors_on_books_they_write
	editors_on_books_from_authors_they_manage
}

###################################
# FINDING INFORMATION FOR DECISION
###################################

# Before taking any decision, we need to infer a bit of information,
# namely which groups the user belongs to (which can also come directly from the Identity Provider)
# as well as which roles it is associated to, either by group mappings or direct user mappings
user_groups contains group if {
	some group in data.directory[input.user].groups
}
user_group_roles contains role if {
	some group in user_groups
	some role in data.group_mappings[group].roles
}
user_direct_roles contains role if {
	some role in data.user_mappings[input.user].roles
}
user_roles := user_group_roles | user_direct_roles

# We also need information about the author potentially associated to the user,
# taking them as a list because we never know what can happen, even though
# the business rules should normally make a user only have one author linked at most
user_author contains author if {
	some author in data.authors
	author.user == input.user
}
#default user_author := []
#user_author := {
#	some author in user_authors
#}

# Same thing for the book, as we need to retrieve it from its id in the list of data
# to be able to make some decision from rules on book attributes
book contains b if {
	some b in data.books[input.book]
}

# In the case of an author, we also have to retrieve the list of book
# they are the writer of, because some rules applies on this as well
author_books contains book if {
	some author in user_author
	some b in data.books
	b.editing.author == author.id
}

#######################
# CHECKING PERMISSIONS
#######################

# These roles information are then used to decide if generic access is granted,
# because permissions should be first obtained, for any kind of user
permission_associated_to_role if {
	some access in user_accesses_by_roles
	access.type == input.resource
	access.operation == input.operation
}

# If the resource requested is the core of the flower, an access granted to any of the petals
# of the flower is enough to grant access to the core, in the referential flower metaphor
permission_associated_to_role if {
	some access in user_accesses_by_roles
	"books" == input.resource
	access.operation == input.operation
}

# If the operation allowed by a role is all, that means any kind of operation is allowed
permission_associated_to_role if {
	some access in user_accesses_by_roles
	access.type == input.resource
	access.operation == "all"
}

user_accesses_by_roles contains access if {
	some role in user_roles
	some access in permissions[role]
}
roles_graph[entity_name] := edges {
	data.roles[entity_name]
	edges := {neighbor | data.roles[neighbor].parent == entity_name}
}
permissions[entity_name] := access {
	data.roles[entity_name]
	reachable := graph.reachable(roles_graph, {entity_name})
	access := {item | reachable[k]; item := data.roles[k].access[_]}
}

###########################################
# CHECKING RULE ON AUTHORS FOR THEIR BOOKS
###########################################

# An author can only have rights on books they are authoring
default authors_on_books_they_write := false
authors_on_books_they_write if {
	some role in user_roles
	role != "book-writer"
}
authors_on_books_they_write if {
	some role in user_roles
	role == "book-writer"
	some author in user_author
	some b in data.books
	b.editing.author == author.id
	b.id == input.book
}

#################################################################
# CHECKING RULE ON EDITORS ON THE BOOKS FROM AUTHORS THEY MANAGE
#################################################################

# An editor only have rights on books from the authors they manage
default editors_on_books_from_authors_they_manage := false
editors_on_books_from_authors_they_manage if {
	some role in user_roles
	role != "book-edition"
}
book_author contains b.editing.author if {
	some b in data.books
	b.id == input.book
}
editors_on_books_from_authors_they_manage if {
	some role in user_roles
	role == "book-edition"
	some author in book_author
	some b in data.books
	b.editing.author == author
	b.id == input.book
	user_hierarchy_ok	
}
# ======================== TODO : changer éventuellement l'org_chart qui devrait contenir des id d'auteurs sous les éditeurs, pas des users
foundpath = path {
	[path, _] := walk(org_chart)
	some author in book_author
    path[_] == author
}
user_hierarchy_ok if {
	some user in foundpath
	user == input.user
}

###############################################
# CHECKING RULE ON SALESPERSON FOR BOOK STATUS
###############################################

# Salespersons cannot see a book if it is not in published or archived status
default no_status_blocking := false
no_status_blocking if {
	some role in user_roles
	role != "book-sales"
}
default readable_for_sales := false
readable_for_sales if {
	book.status == "published"
}
readable_for_sales if {
	book.status == "archived"
}
no_status_blocking if {
	some role in user_roles
	role == "book-sales"
	readable_for_sales
}

#####################################################
# CHECKING RULE ON AUTHORS BEING POTENTIALLY BLOCKED
#####################################################

# If an author has been blocked, they cannot have any access to a book
default no_author_blocking := false
no_author_blocking if {
	some role in user_roles
	role != "book-writer"
}
no_author_blocking if {
	some role in user_roles
	role == "book-writer"
	some user in user_author
	user.restriction == "none"
}
