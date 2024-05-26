package notification

import future.keywords

# First we determine what is the order of preference of the channels
# This is mainly where the priority is taken into account, since we consider
# for now that it does not influence the default order, but just the preferences
# if the user concerned by the notification has set them
default order := ["portal", "email", "sms"]
order = data.destination.preferences[input.moment][input.priority]

# Then we gather existing data for the channels, since there is no used
# in proposing to notify on this channel if the receiver does not own them
default portal := {}
default cellphone := {}
default workemail := {}
default homeemail := {}
default senderemail := {}
portal = data.destination.portal
cellphone = data.destination.phones.cell
workemail = data.destination.emails.work
homeemail = data.destination.emails.home
senderemail = data.origin.emails.work

# In order to retrieve the right information, we are going to follow the list
# of preferences and recreate another array in the same order, but with the data
# associated to the channel if it is provided
default indexes := { 0, 1, 2 }

# To notify through the portal, we simply need the portal information of the user
channels[valeur] = [order[valeur], portal] {
    some valeur in indexes
    order[valeur] = "portal"
}

# Notifying through SMS needs a cellular phone, may it be work associated or personal phone
channels[valeur] = [order[valeur], cellphone] {
    some valeur in indexes
    order[valeur] = "sms"
}

# Sending an email to the person is a bit special, because it needs also that the origin
# of the notification has an email indicated in its coordinates (even if it is a noreply address)
channels[valeur] = [order[valeur], workemail] {
    some valeur in indexes
    order[valeur] = "email"
    input.moment = "workday"
    senderemail != {}
}

# Email notification has also another peculiarity, namely that the address may be another one
# depending on the moment: the person only specifies an "email" preference but it is in the rules
# system that we determine which email address (home or work) we are going to use
channels[valeur] = [order[valeur], homeemail] {
    some valeur in indexes
    order[valeur] = "email"
    input.moment != "workday"
    senderemail != {}
}

# There must be a better way to retrieve the first entity in an array when the indexes
# do not follow each other but I have not found how to do so
default channel := []
channel = channels[0] {
    channels[0][1] != {}
}
channel := channels[1] {
    channels[0][1] = {}
    channels[1][1] != {}
}
channel := channels[2] {
    channels[0][1] = {}
    channels[1][1] = {}
    channels[2][1] != {}
}
