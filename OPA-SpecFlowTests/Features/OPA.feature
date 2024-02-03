Feature: OPA

Scenario: An author has all rights on the content of their book
	Given book number 978-2409002205 with author id 00024 is in workInProgress status
	And user jpgou belongs to group authors
	And organizational chart is {"frfra":{"frvel":{},"cemol":{},"mnfra":{"00024":{},"00025":{}}}}
	And user jpgou is associated to author 00024 who has level of restriction none
	When user jpgou request all access to the books.content petal of the book number 978-2409002205
	Then access should be accepted

Scenario: An author has no rights on the content of the book from another author
	Given book number 978-2409002205 with author id 00024 is in workInProgress status
	And user jpgou belongs to group authors
	And organizational chart is {"frfra":{"frvel":{},"cemol":{},"mnfra":{"00024":{},"00025":{}}}}
	And user jpgou is associated to author 00024 who has level of restriction none
	When user nicou request read access to the books.content petal of the book number 978-2409002205
	Then access should be refused

Scenario: Au author that has been blocked has no rights, even on their own books
	Given book number 978-2409002205 with author id 00024 is in workInProgress status
	And user jpgou belongs to group authors
	And organizational chart is {"frfra":{"frvel":{},"cemol":{},"mnfra":{"00024":{},"00025":{}}}}
	And user jpgou is associated to author 00024 who has level of restriction blocked
	When user jpgou request all access to the books.content petal of the book number 978-2409002205
	Then access should be refused

Scenario: An editor has all rights on the content of the books from the authors they manage
	Given book number 978-2409002205 with author id 00024 is in workInProgress status
	And user jpgou belongs to group authors
	And user mnfra belongs to group editors
	And organizational chart is {"frfra":{"frvel":{},"cemol":{},"mnfra":{"00024":{},"00025":{}}}}
	And user jpgou is associated to author 00024 who has level of restriction none
	When user mnfra request all access to the books.content petal of the book number 978-2409002205
	Then access should be accepted

Scenario: An editor has no rights on the content of the books from the authors they do not manage
	Given book number 978-2409002205 with author id 00024 is in workInProgress status
	And user jpgou belongs to group authors
	And user mnfra belongs to group editors
	And organizational chart is {"frfra":{"frvel":{},"cemol":{},"mnfra":{"nicou":{}}}}
	And user jpgou is associated to author 00024 who has level of restriction none
	When user mnfra request all access to the books.content petal of the book number 978-2409002205
	Then access should be refused

Scenario: Refusing salesperson access to work-in-progress book
	Given book number 978-2409002205 with author id 00024 is in workInProgress status
	And user frvel belongs to group commerce
	And organizational chart is {"frfra":{"frvel":{},"cemol":{},"mnfra":{"00024":{},"00025":{}}}}
	When user frvel request read access to the books.content petal of the book number 978-2409002205
	Then access should be refused
