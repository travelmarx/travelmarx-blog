class SimpleClass(object):
    """A simple example class"""
    i = 12345

    def f(self):
        return 'hello world'

class Person(object):
    """Creates a Person based on name and age."""
    def __init__(self, name, age):
        self.name = name
        self.age = age
